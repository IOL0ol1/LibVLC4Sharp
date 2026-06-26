using System;
using System.Collections;
using System.Collections.Generic;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>
    /// A media list (<c>libvlc_media_list_t</c>): an ordered, lockable collection of
    /// <see cref="Media"/> descriptors.
    /// </summary>
    public unsafe class MediaList : NativeReference, IReadOnlyList<Media>
    {
        // libvlc 4.0 removed the media-list event manager (and there is no libvlc_media_list_cbs), so
        // MediaList no longer raises ItemAdded/ItemDeleted events.

        /// <summary>Creates an empty media list. <c>libvlc_media_list_new</c>.</summary>
        /// <returns>Empty media list, or NULL on error.</returns>
        public MediaList() : base((IntPtr)libvlc_media_list_new()) { }

        /// <summary>Wraps an existing native handle.</summary>
        /// <param name="handle">Native <c>libvlc_media_list_t*</c>.</param>
        public MediaList(IntPtr handle) : base(handle) { }

        /// <summary>Implicit conversion to the native <c>libvlc_media_list_t*</c> (null for a null list).</summary>
        public static implicit operator libvlc_media_list_t*(MediaList? mediaList) =>
            mediaList is null ? null : (libvlc_media_list_t*)mediaList.NativeHandle;

        protected override void Release(IntPtr handle) =>
            libvlc_media_list_release((libvlc_media_list_t*)handle);

        /// <summary>Number of items in the media list. <c>libvlc_media_list_count</c>.</summary>
        /// <remarks>The list lock should be held when reading this value.</remarks>
        public int Count => libvlc_media_list_count(this);

        /// <summary>
        /// Indicates whether this media list is read-only from a user point of view.
        /// <c>libvlc_media_list_is_readonly</c>.
        /// </summary>
        /// <returns><c>true</c> if read-only, <c>false</c> if read/write.</returns>
        public bool IsReadOnly => libvlc_media_list_is_readonly(this).ToBool();

        /// <summary>Acquires the lock on media list items. <c>libvlc_media_list_lock</c>.</summary>
        /// <remarks>
        /// Must be held when calling <see cref="Count"/>, <see cref="Add"/>,
        /// <see cref="Insert"/>, <see cref="RemoveAt"/>, <see cref="this[int]"/>,
        /// and <see cref="IndexOf"/> to protect concurrent modifications.
        /// </remarks>
        public void Lock() => libvlc_media_list_lock(this);

        /// <summary>Releases the lock on media list items. <c>libvlc_media_list_unlock</c>.</summary>
        /// <remarks>The list lock must be held upon calling this function.</remarks>
        public void Unlock() => libvlc_media_list_unlock(this);

        private Media? _media;

        /// <summary>
        /// The media instance associated with this media list.
        /// Setter: <c>libvlc_media_list_set_media</c> — if another media instance was previously
        /// associated it will be released. The list lock must <b>not</b> be held when setting.
        /// Getter: <c>libvlc_media_list_media</c> — returns the assigned instance (the extra
        /// reference the getter takes is released); a new owning wrapper if changed externally.
        /// The list lock must <b>not</b> be held when getting.
        /// </summary>
        public Media? Media
        {
            get => Reconcile(ref _media, (IntPtr)libvlc_media_list_media(this), // +1 ref, or null
                static h => libvlc_media_release((libvlc_media_t*)h),
                static h => new Media(h));
            set
            {
                _media = value;
                libvlc_media_list_set_media(this, value);
            }
        }

        /// <summary>Appends a media instance to the media list. <c>libvlc_media_list_add_media</c>.</summary>
        /// <param name="media">The media instance to add.</param>
        /// <returns>0 on success, -1 if the media list is read-only.</returns>
        /// <remarks>The list lock should be held upon calling this function.</remarks>
        public int Add(Media media) => libvlc_media_list_add_media(this, media);

        /// <summary>
        /// Inserts a media instance into the media list at the given position.
        /// <c>libvlc_media_list_insert_media</c>.
        /// </summary>
        /// <param name="index">Position in the list at which to insert.</param>
        /// <param name="media">The media instance to insert.</param>
        /// <returns>0 on success, -1 if the media list is read-only.</returns>
        /// <remarks>The list lock should be held upon calling this function.</remarks>
        public int Insert(int index, Media media) => libvlc_media_list_insert_media(this, media, index);

        /// <summary>
        /// Removes the media instance at the given position from the media list.
        /// <c>libvlc_media_list_remove_index</c>.
        /// </summary>
        /// <param name="index">Position of the item to remove.</param>
        /// <returns>0 on success, -1 if the list is read-only or the item was not found.</returns>
        /// <remarks>The list lock should be held upon calling this function.</remarks>
        public int RemoveAt(int index) => libvlc_media_list_remove_index(this, index);

        /// <summary>
        /// Finds the index position of a media instance in the list.
        /// <c>libvlc_media_list_index_of_item</c>.
        /// </summary>
        /// <param name="media">The media instance to find.</param>
        /// <returns>Position of the media instance, or -1 if the media was not found.</returns>
        /// <remarks>
        /// Returns the first matched position. The list lock should be held upon calling this
        /// function.
        /// </remarks>
        public int IndexOf(Media media) => libvlc_media_list_index_of_item(this, media);

        /// <summary>
        /// Returns the media instance at the given position (a new owned reference — dispose it).
        /// <c>libvlc_media_list_item_at_index</c>.
        /// </summary>
        /// <param name="index">Position in the list.</param>
        /// <returns>
        /// Media instance at position <paramref name="index"/>. Throws
        /// <see cref="ArgumentOutOfRangeException"/> if not found.
        /// </returns>
        /// <remarks>The list lock should be held upon calling this function.</remarks>
        public Media this[int index]
        {
            get
            {
                var m = libvlc_media_list_item_at_index(this, index);
                return m == null ? throw new ArgumentOutOfRangeException() : new Media((IntPtr)m);
            }
        }

        /// <summary>Snapshots the list (under lock) into owned <see cref="Media"/> references.</summary>
        public IEnumerator<Media> GetEnumerator()
        {
            var items = new List<Media>();
            Lock();
            try
            {
                int count = Count;
                for (int i = 0; i < count; i++)
                {
                    var m = libvlc_media_list_item_at_index(this, i);
                    if (m != null) items.Add(new Media((IntPtr)m));
                }
            }
            finally { Unlock(); }
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
