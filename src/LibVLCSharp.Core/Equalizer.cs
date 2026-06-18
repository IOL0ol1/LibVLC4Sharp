using System;
using System.Collections.Generic;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>An audio equalizer (<c>libvlc_equalizer_t</c>); assign to <see cref="MediaPlayer.Equalizer"/>.</summary>
    public unsafe class Equalizer : NativeReference
    {
        /// <summary>Creates a flat equalizer. <c>libvlc_audio_equalizer_new</c>.</summary>
        public Equalizer() : base((IntPtr)libvlc_audio_equalizer_new()) { }

        /// <summary>Wraps an existing native handle.</summary>
        /// <param name="handle">Native <c>libvlc_equalizer_t*</c>.</param>
        public Equalizer(IntPtr handle) : base(handle) { }

        /// <summary>Creates an equalizer from a built-in preset. <c>libvlc_audio_equalizer_new_from_preset</c>.</summary>
        public static Equalizer FromPreset(uint index) =>
            new Equalizer((IntPtr)libvlc_audio_equalizer_new_from_preset(index));

        /// <summary>Implicit conversion to the native <c>libvlc_equalizer_t*</c> (null for a null equalizer).</summary>
        public static implicit operator libvlc_equalizer_t*(Equalizer? equalizer) =>
            equalizer is null ? null : (libvlc_equalizer_t*)equalizer.NativeHandle;

        protected override void Release(IntPtr handle) => libvlc_audio_equalizer_release((libvlc_equalizer_t*)handle);

        /// <summary>Pre-amplification in dB. <c>libvlc_audio_equalizer_get_preamp</c> / <c>set_preamp</c>.</summary>
        public float PreAmp
        {
            get => libvlc_audio_equalizer_get_preamp(this);
            set => libvlc_audio_equalizer_set_preamp(this, value);
        }

        /// <summary>Amplification (dB) of a band. <c>libvlc_audio_equalizer_get_amp_at_index</c>.</summary>
        public float GetAmp(uint band) => libvlc_audio_equalizer_get_amp_at_index(this, band);

        /// <summary>Sets a band's amplification (dB). <c>libvlc_audio_equalizer_set_amp_at_index</c>. Returns 0 on success.</summary>
        public int SetAmp(uint band, float amp) => libvlc_audio_equalizer_set_amp_at_index(this, amp, band);

        public static IReadOnlyList<float> BandFrequencies 
            = new ReadOnlyList<float>(() => (int)libvlc_audio_equalizer_get_band_count(), i => libvlc_audio_equalizer_get_band_frequency((uint)i));

        public static IReadOnlyList<string?> PresetNames 
            = new ReadOnlyList<string?>(() => (int)libvlc_audio_equalizer_get_preset_count(), i => ((IntPtr)libvlc_audio_equalizer_get_preset_name((uint)i)).GetUtf8());

        private class ReadOnlyList<T> : IReadOnlyList<T>
        {
            private Func<int> getCount;
            private Func<int, T> getItem;
            public ReadOnlyList(Func<int> getCount, Func<int, T> getItem)
            {
                this.getCount = getCount;
                this.getItem = getItem;
            }
            public int Count => getCount();
            public T this[int index] => getItem(index) ?? throw new ArgumentOutOfRangeException();
            public IEnumerator<T> GetEnumerator()
            {
                var count = Count;
                for (var i = 0; i < count; i++)
                    yield return this[i];
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
 
    }
}
