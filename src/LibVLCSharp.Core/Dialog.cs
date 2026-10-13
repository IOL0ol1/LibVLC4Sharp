using System;
using System.Runtime.InteropServices;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{

    /// <summary>
    /// Routes libvlc's interaction dialogs (login, question, progress) for a <see cref="LibVLC"/>
    /// instance to managed events. Registers <c>libvlc_dialog_set_callbacks</c> on construction and
    /// clears it on <see cref="Release"/>.
    /// </summary>
    /// <remarks>
    /// libvlc allows a single set of dialog callbacks per instance, so this is created lazily and
    /// owned by its <see cref="LibVLC"/> — obtain it via <see cref="LibVLC.Dialog"/> rather than
    /// constructing it. Events fire on libvlc's own threads — marshal to your UI thread in handlers
    /// as needed. It is disposed automatically when the owning <see cref="LibVLC"/> is disposed.
    /// </remarks>
    public unsafe class Dialog
    {
        // Native signatures of the libvlc_dialog_cbs function pointers. Strings arrive as raw UTF-8
        // pointers (read with GetUtf8); _Bool arrives as a single byte.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DisplayLoginCb(IntPtr data, libvlc_dialog_id* id, IntPtr title, IntPtr text, IntPtr defaultUsername, byte askStore);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DisplayQuestionCb(IntPtr data, libvlc_dialog_id* id, IntPtr title, IntPtr text, libvlc_dialog_question_type type, IntPtr cancel, IntPtr action1, IntPtr action2);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DisplayProgressCb(IntPtr data, libvlc_dialog_id* id, IntPtr title, IntPtr text, byte indeterminate, float position, IntPtr cancel);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CancelCb(IntPtr data, libvlc_dialog_id* id);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void UpdateProgressCb(IntPtr data, libvlc_dialog_id* id, float position, IntPtr text);

        // Captured at construction so Release can clear the callbacks even after the owning LibVLC
        // has zeroed its NativeHandle (LibVLC disposes this from its Release, before libvlc_release).
        private readonly libvlc_instance_t* _instance;

        // Kept alive for the object's lifetime; libvlc holds the function pointers.
        private readonly DisplayLoginCb _onLogin;
        private readonly DisplayQuestionCb _onQuestion;
        private readonly DisplayProgressCb _onProgress;
        private readonly CancelCb _onCancel;
        private readonly UpdateProgressCb _onUpdateProgress;
        private readonly DialogErrorCallbacks _onError;

        private bool _disposed;

        // Guards lazy (un)registration and the subscriber-backing fields below. Registration is
        // attach-on-first / detach-on-last: the 5 dialog callbacks share _dialogRegistered (one struct),
        // the error callback has its own _errorRegistered.
        private readonly object _gate = new object();
        private bool _dialogRegistered;
        private bool _errorRegistered;

        private EventHandler<DialogLoginEventArgs>? _loginRequested;
        private EventHandler<DialogQuestionEventArgs>? _questionRequested;
        private EventHandler<DialogProgressEventArgs>? _progressDisplayed;
        private EventHandler<DialogProgressUpdateEventArgs>? _progressUpdated;
        private EventHandler<DialogCancelEventArgs>? _cancelled;
        private EventHandler<DialogErrorEventArgs>? _errorRequested;

        /// <summary>
        /// Prepares the dialog handler for <paramref name="libVLC"/>. No libvlc callback is registered
        /// until a managed event actually gets a subscriber (see the event accessors), so merely
        /// obtaining the handler does not take over libvlc's default dialog handling.
        /// </summary>
        internal Dialog(LibVLC libVLC)
        {
            if (libVLC is null) throw new ArgumentNullException(nameof(libVLC));
            _instance = libVLC; // implicit conversion to libvlc_instance_t*

            // Roots the delegates for the object's lifetime; libvlc only receives the function pointers
            // once registration happens lazily on the first subscriber.
            _onLogin = OnLogin;
            _onQuestion = OnQuestion;
            _onProgress = OnProgress;
            _onCancel = OnCancel;
            _onUpdateProgress = OnUpdateProgress;
            _onError = OnError;
        }

        /// <summary>Raised when libvlc needs a username/password.</summary>
        public event EventHandler<DialogLoginEventArgs>? LoginRequested
        {
            add => AddDialog(ref _loginRequested, value);
            remove => RemoveDialog(ref _loginRequested, value);
        }

        /// <summary>Raised when libvlc asks a question with up to two actions.</summary>
        public event EventHandler<DialogQuestionEventArgs>? QuestionRequested
        {
            add => AddDialog(ref _questionRequested, value);
            remove => RemoveDialog(ref _questionRequested, value);
        }

        /// <summary>Raised when libvlc starts a progress dialog.</summary>
        public event EventHandler<DialogProgressEventArgs>? ProgressDisplayed
        {
            add => AddDialog(ref _progressDisplayed, value);
            remove => RemoveDialog(ref _progressDisplayed, value);
        }

        /// <summary>Raised when a progress dialog advances.</summary>
        public event EventHandler<DialogProgressUpdateEventArgs>? ProgressUpdated
        {
            add => AddDialog(ref _progressUpdated, value);
            remove => RemoveDialog(ref _progressUpdated, value);
        }

        /// <summary>Raised when a dialog should be cancelled/closed.</summary>
        public event EventHandler<DialogCancelEventArgs>? Cancelled
        {
            add => AddDialog(ref _cancelled, value);
            remove => RemoveDialog(ref _cancelled, value);
        }

        /// <summary>Raised when libvlc reports an error to show the user. <c>libvlc_dialog_set_error_callback</c>.</summary>
        public event EventHandler<DialogErrorEventArgs>? ErrorRequested
        {
            add
            {
                lock (_gate)
                {
                    if (_disposed) return;
                    _errorRequested += value;
                    if (!_errorRegistered)
                    {
                        libvlc_dialog_set_error_callback(_instance, _onError.ToFunctionPointer(), IntPtr.Zero);
                        _errorRegistered = true;
                    }
                }
            }
            remove
            {
                lock (_gate)
                {
                    _errorRequested -= value;
                    if (_errorRegistered && _errorRequested is null)
                    {
                        libvlc_dialog_set_error_callback(_instance, IntPtr.Zero, IntPtr.Zero);
                        _errorRegistered = false;
                    }
                }
            }
        }

        // libvlc takes the 5 dialog callbacks as one all-or-nothing struct, so any of these five events
        // having a subscriber keeps the single registration alive; it is cleared when the last of them
        // unsubscribes. (The error callback is a separate libvlc registration, gated independently.)
        private void AddDialog<T>(ref EventHandler<T>? field, EventHandler<T>? value)
        {
            lock (_gate)
            {
                if (_disposed) return;
                field += value;
                if (!_dialogRegistered)
                {
                    var cbs = new libvlc_dialog_cbs
                    {
                        pf_display_login = _onLogin.ToFunctionPointer(),
                        pf_display_question = _onQuestion.ToFunctionPointer(),
                        pf_display_progress = _onProgress.ToFunctionPointer(),
                        pf_cancel = _onCancel.ToFunctionPointer(),
                        pf_update_progress = _onUpdateProgress.ToFunctionPointer(),
                    };
                    libvlc_dialog_set_callbacks(_instance, &cbs, IntPtr.Zero);
                    _dialogRegistered = true;
                }
            }
        }

        private void RemoveDialog<T>(ref EventHandler<T>? field, EventHandler<T>? value)
        {
            lock (_gate)
            {
                field -= value;
                if (_dialogRegistered && _loginRequested is null && _questionRequested is null
                    && _progressDisplayed is null && _progressUpdated is null && _cancelled is null)
                {
                    libvlc_dialog_set_callbacks(_instance, null, IntPtr.Zero);
                    _dialogRegistered = false;
                }
            }
        }

        private void OnLogin(IntPtr data, libvlc_dialog_id* id, IntPtr title, IntPtr text, IntPtr defaultUsername, byte askStore) =>
            _loginRequested?.Invoke(this, new DialogLoginEventArgs(
                new DialogId(id), title.GetUtf8(), text.GetUtf8(), defaultUsername.GetUtf8(), askStore.ToBool()));

        private void OnQuestion(IntPtr data, libvlc_dialog_id* id, IntPtr title, IntPtr text, libvlc_dialog_question_type type, IntPtr cancel, IntPtr action1, IntPtr action2) =>
            _questionRequested?.Invoke(this, new DialogQuestionEventArgs(
                new DialogId(id), title.GetUtf8(), text.GetUtf8(), (DialogQuestionType)type,
                cancel.GetUtf8(), action1.GetUtf8(), action2.GetUtf8()));

        private void OnProgress(IntPtr data, libvlc_dialog_id* id, IntPtr title, IntPtr text, byte indeterminate, float position, IntPtr cancel) =>
            _progressDisplayed?.Invoke(this, new DialogProgressEventArgs(
                new DialogId(id), title.GetUtf8(), text.GetUtf8(), indeterminate.ToBool(), position, cancel.GetUtf8()));

        private void OnUpdateProgress(IntPtr data, libvlc_dialog_id* id, float position, IntPtr text) =>
            _progressUpdated?.Invoke(this, new DialogProgressUpdateEventArgs(new DialogId(id), position, text.GetUtf8()));

        private void OnCancel(IntPtr data, libvlc_dialog_id* id) =>
            _cancelled?.Invoke(this, new DialogCancelEventArgs(new DialogId(id)));

        private void OnError(IntPtr data, byte* title, byte* text) =>
            _errorRequested?.Invoke(this, new DialogErrorEventArgs(((IntPtr)title).GetUtf8(), ((IntPtr)text).GetUtf8()));

        /// <summary>
        /// Clears any registered dialog/error callbacks on the owning instance and permanently disables
        /// this handler. <b>Internal — owned by <see cref="LibVLC"/> and called from its dispose path.</b>
        /// To stop receiving dialogs, unsubscribe the events instead (registration is then cleared
        /// automatically). <c>libvlc_dialog_set_callbacks(null)</c>.
        /// </summary>
        internal void Release()
        {
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                if (_dialogRegistered)
                {
                    libvlc_dialog_set_callbacks(_instance, null, IntPtr.Zero);
                    _dialogRegistered = false;
                }
                if (_errorRegistered)
                {
                    libvlc_dialog_set_error_callback(_instance, IntPtr.Zero, IntPtr.Zero);
                    _errorRegistered = false;
                }
            }
        }
    }

    /// <summary>
    /// Identifies a single in-flight libvlc dialog (<c>libvlc_dialog_id*</c>). Passed to the
    /// <see cref="Dialog"/> events; respond to the dialog through its methods.
    /// </summary>
    public readonly unsafe struct DialogId
    {
        private readonly IntPtr _handle;

        internal DialogId(libvlc_dialog_id* id) => _handle = (IntPtr)id;

        /// <summary>Implicit conversion to the native <c>libvlc_dialog_id*</c>.</summary>
        public static implicit operator libvlc_dialog_id*(DialogId id) => (libvlc_dialog_id*)id._handle;

        /// <summary>
        /// Opaque pointer associated with this dialog id. The getter wraps
        /// <c>libvlc_dialog_get_context</c>; the setter wraps
        /// <c>libvlc_dialog_set_context</c>. Available since LibVLC 3.0.0.
        /// </summary>
        public IntPtr Context
        {
            get => libvlc_dialog_get_context(this);
            set => libvlc_dialog_set_context(this, value);
        }

        /// <summary>
        /// Posts a login answer. After this call the dialog id is no longer valid.
        /// <c>libvlc_dialog_post_login</c>. Available since LibVLC 3.0.0.
        /// </summary>
        /// <param name="username">Valid, non-empty username string.</param>
        /// <param name="password">Password string (may be empty).</param>
        /// <param name="store">If <see langword="true"/>, store the credentials.</param>
        /// <returns>0 on success, or -1 on error.</returns>
        public int PostLogin(string username, string password, bool store)
        {
            using var uUser = new Utf8Buffer(username);
            using var uPass = new Utf8Buffer(password);
            return libvlc_dialog_post_login(this, uUser, uPass, store.ToByte());
        }

        /// <summary>
        /// Posts a question answer. After this call the dialog id is no longer valid.
        /// <c>libvlc_dialog_post_action</c>. Available since LibVLC 3.0.0.
        /// </summary>
        /// <param name="action">1 for action1, 2 for action2.</param>
        /// <returns>0 on success, or -1 on error.</returns>
        public int PostAction(int action) => libvlc_dialog_post_action(this, action);

        /// <summary>
        /// Dismisses a dialog. After this call the dialog id is no longer valid.
        /// <c>libvlc_dialog_dismiss</c>. Available since LibVLC 3.0.0.
        /// </summary>
        /// <returns>0 on success, or -1 on error.</returns>
        public int Dismiss() => libvlc_dialog_dismiss(this);
    }

    /// <summary>
    /// Raised when a login dialog needs to be displayed (<c>pf_display_login</c>).
    /// Respond with <see cref="DialogId.PostLogin"/> or <see cref="DialogId.Dismiss"/>.
    /// </summary>
    public readonly struct DialogLoginEventArgs
    {
        /// <summary>Id used to interact with the dialog.</summary>
        public readonly DialogId Id;
        /// <summary>Title of the dialog.</summary>
        public readonly string? Title;
        /// <summary>Text of the dialog.</summary>
        public readonly string? Text;
        /// <summary>Username that should be pre-filled on the user form, or null.</summary>
        public readonly string? DefaultUsername;
        /// <summary>
        /// If <see langword="true"/>, ask the user whether to save the credentials.
        /// </summary>
        public readonly bool AskStore;
        public DialogLoginEventArgs(DialogId id, string? title, string? text, string? defaultUsername, bool askStore)
        {
            Id = id; Title = title; Text = text; DefaultUsername = defaultUsername; AskStore = askStore;
        }
    }

    /// <summary>
    /// Raised when a question dialog needs to be displayed (<c>pf_display_question</c>).
    /// Respond with <see cref="DialogId.PostAction"/> or <see cref="DialogId.Dismiss"/>.
    /// </summary>
    public readonly struct DialogQuestionEventArgs
    {
        /// <summary>Id used to interact with the dialog.</summary>
        public readonly DialogId Id;
        /// <summary>Title of the dialog.</summary>
        public readonly string? Title;
        /// <summary>Text of the dialog.</summary>
        public readonly string? Text;
        /// <summary>Question type (severity) of the dialog.</summary>
        public readonly DialogQuestionType QuestionType;
        /// <summary>Text of the cancel button.</summary>
        public readonly string? Cancel;
        /// <summary>Text of the first action button. If null, this button should not be displayed.</summary>
        public readonly string? Action1;
        /// <summary>Text of the second action button. If null, this button should not be displayed.</summary>
        public readonly string? Action2;
        public DialogQuestionEventArgs(DialogId id, string? title, string? text, DialogQuestionType type,
            string? cancel, string? action1, string? action2)
        {
            Id = id; Title = title; Text = text; QuestionType = type;
            Cancel = cancel; Action1 = action1; Action2 = action2;
        }
    }

    /// <summary>
    /// Raised when a progress dialog needs to be displayed (<c>pf_display_progress</c>). If
    /// cancellable (<see cref="Cancel"/> is not null), the dialog may be cancelled by calling
    /// <see cref="DialogId.Dismiss"/>.
    /// </summary>
    public readonly struct DialogProgressEventArgs
    {
        /// <summary>Id used to interact with the dialog.</summary>
        public readonly DialogId Id;
        /// <summary>Title of the dialog.</summary>
        public readonly string? Title;
        /// <summary>Text of the dialog.</summary>
        public readonly string? Text;
        /// <summary><see langword="true"/> if the progress dialog is indeterminate.</summary>
        public readonly bool Indeterminate;
        /// <summary>Initial position of the progress bar (between 0.0 and 1.0).</summary>
        public readonly float Position;
        /// <summary>Text of the cancel button. If null, the dialog is not cancellable.</summary>
        public readonly string? Cancel;
        public DialogProgressEventArgs(DialogId id, string? title, string? text, bool indeterminate, float position, string? cancel)
        {
            Id = id; Title = title; Text = text; Indeterminate = indeterminate; Position = position; Cancel = cancel;
        }
    }

    /// <summary>Raised when a displayed progress dialog needs to be updated (<c>pf_update_progress</c>).</summary>
    public readonly struct DialogProgressUpdateEventArgs
    {
        /// <summary>Id of the dialog being updated.</summary>
        public readonly DialogId Id;
        /// <summary>New position of the progress bar (between 0.0 and 1.0).</summary>
        public readonly float Position;
        /// <summary>New text of the progress dialog, or null.</summary>
        public readonly string? Text;
        public DialogProgressUpdateEventArgs(DialogId id, float position, string? text)
        {
            Id = id; Position = position; Text = text;
        }
    }

    /// <summary>
    /// Raised when a displayed dialog needs to be cancelled (<c>pf_cancel</c>). The implementation
    /// must call <see cref="DialogId.Dismiss"/> to actually release the dialog.
    /// </summary>
    public readonly struct DialogCancelEventArgs
    {
        /// <summary>Id of the dialog being cancelled.</summary>
        public readonly DialogId Id;
        public DialogCancelEventArgs(DialogId id) => Id = id;
    }

    /// <summary>
    /// Raised when an error message needs to be displayed (<c>libvlc_dialog_error_cbs</c>).
    /// Available since LibVLC 4.0.0.
    /// </summary>
    public readonly struct DialogErrorEventArgs
    {
        /// <summary>Title of the error dialog.</summary>
        public readonly string? Title;
        /// <summary>Text of the error dialog.</summary>
        public readonly string? Text;
        public DialogErrorEventArgs(string? title, string? text) { Title = title; Text = text; }
    }

}
