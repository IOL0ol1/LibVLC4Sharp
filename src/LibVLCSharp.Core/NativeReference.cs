using System;
using System.Threading;

namespace LibVLCSharp.Core
{
    /// <summary>
    /// Base for the thin wrappers around reference-counted libvlc objects. Owns the native handle and
    /// releases it once (on <see cref="Dispose()"/> or finalization) via <see cref="Release"/>; pass
    /// <c>owns: false</c> to wrap a borrowed handle that must not be released.
    /// </summary>
    public abstract class NativeReference : IDisposable
    {
        private IntPtr _handle;
        private readonly bool _owns;

        private protected NativeReference(IntPtr handle, bool owns = true)
        {
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException($"{GetType().Name}: libvlc returned a null handle.");
            _handle = handle;
            _owns = owns;
            if (!owns) GC.SuppressFinalize(this);
        }

        /// <summary>The native <c>libvlc_*_t*</c>; <see cref="IntPtr.Zero"/> once disposed.</summary>
        public IntPtr NativeHandle => _handle;

        /// <summary>Frees the handle (the matching <c>libvlc_*_release</c>). Unmanaged-only — also runs on the finalizer.</summary>
        protected abstract void Release(IntPtr handle);

        /// <summary>
        /// Release-pattern core: frees the handle once, if owned. Override to dispose managed members
        /// (guard with <paramref name="disposing"/>, placed before/after the <c>base</c> call as ordering needs).
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            var h = Interlocked.Exchange(ref _handle, IntPtr.Zero);
            if (h != IntPtr.Zero && _owns) Release(h);
        }

        /// <summary>Reconciles a cached child wrapper against a "+1 ref" getter result (releases the extra ref on a cache hit).</summary>
        private protected static T? Reconcile<T>(ref T? cache, IntPtr handle, Action<IntPtr> release, Func<IntPtr, T> wrap)
            where T : NativeReference
        {
            if (handle == IntPtr.Zero) return cache = null;
            if (cache != null && cache.NativeHandle == handle) { release(handle); return cache; }
            return cache = wrap(handle);
        }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        ~NativeReference() => Dispose(false);
    }
}
