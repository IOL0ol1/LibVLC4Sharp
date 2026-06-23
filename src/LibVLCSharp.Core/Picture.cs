using System;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>A picture (<c>libvlc_picture_t</c>), e.g. a thumbnail or snapshot.</summary>
    public unsafe class Picture : NativeReference
    {
        /// <summary>Wraps an owned native handle (released on dispose).</summary>
        /// <param name="handle">Native <c>libvlc_picture_t*</c>.</param>
        public Picture(IntPtr handle) : base(handle) { }

        /// <summary>
        /// Wraps a native handle. When <paramref name="addRef"/> is <c>false</c> the picture is a borrowed
        /// view that is never released and is valid only for as long as its owner keeps it alive
        /// (e.g. a picture obtained from <see cref="AttachedThumbnailsFoundEventArgs.GetThumbnails"/> with
        /// <c>owner: false</c>, valid only inside the event handler). Do not dispose a borrowed picture.
        /// </summary>
        /// <param name="handle">Native <c>libvlc_picture_t*</c>.</param>
        /// <param name="addRef"><c>true</c> to retain ownership and release on dispose; <c>false</c> for a borrowed view.</param>
        public Picture(IntPtr handle, bool addRef) : base(handle, addRef) { }

        /// <summary>Implicit conversion to the native <c>libvlc_picture_t*</c> (null for a null picture).</summary>
        public static implicit operator libvlc_picture_t*(Picture? picture) =>
            picture is null ? null : (libvlc_picture_t*)picture.NativeHandle;

        protected override void Release(IntPtr handle) => libvlc_picture_release((libvlc_picture_t*)handle);

        /// <summary>Returns the pixel/encoding type of the picture. <c>libvlc_picture_type</c>.</summary>
        /// <returns>The <see cref="PictureType"/> of the picture.</returns>
        public PictureType Type => (PictureType)libvlc_picture_type(this);

        /// <summary>Returns the time at which this picture was generated, in milliseconds. <c>libvlc_picture_get_time</c>.</summary>
        /// <returns>The media time in milliseconds.</returns>
        public long Time => libvlc_picture_get_time(this);

        /// <summary>Returns the width of the image in pixels. <c>libvlc_picture_get_width</c>.</summary>
        /// <returns>The image width in pixels.</returns>
        public uint Width => libvlc_picture_get_width(this);

        /// <summary>Returns the height of the image in pixels. <c>libvlc_picture_get_height</c>.</summary>
        /// <returns>The image height in pixels.</returns>
        public uint Height => libvlc_picture_get_height(this);

        /// <summary>
        /// Returns the image stride, i.e. the number of bytes per line. <c>libvlc_picture_get_stride</c>.
        /// </summary>
        /// <remarks>
        /// This can only be called on pictures of type <see cref="PictureType.Argb"/> or
        /// <see cref="PictureType.Rgba"/>.
        /// </remarks>
        /// <returns>The row stride in bytes.</returns>
        public uint Stride => libvlc_picture_get_stride(this);

        /// <summary>
        /// Returns the image internal buffer, including potential padding. <c>libvlc_picture_get_buffer</c>.
        /// </summary>
        /// <remarks>
        /// The picture addRef the returned buffer; do not modify or free it. The span is valid only
        /// while this <see cref="Picture"/> is alive.
        /// </remarks>
        /// <returns>A read-only span over the internal picture buffer.</returns>
        public ReadOnlySpan<byte> Buffer
        {
            get
            {
                UIntPtr s;
                var p = libvlc_picture_get_buffer(this, &s);
                return new ReadOnlySpan<byte>(p, (int)s);
            }
        }

        /// <summary>
        /// Saves this picture to a file. The image format matches the type returned by
        /// <see cref="Type"/>. <c>libvlc_picture_save</c>.
        /// </summary>
        /// <param name="path">The path to the generated file.</param>
        /// <returns>0 on success, -1 otherwise.</returns>
        public int Save(string path)
        {
            using var u = new Utf8Buffer(path);
            return libvlc_picture_save(this, u);
        }
    }
}
