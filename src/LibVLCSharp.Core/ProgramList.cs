using System;
using System.Collections;
using System.Collections.Generic;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>
    /// An immutable snapshot of a player program (<c>libvlc_player_program_t</c>) — e.g. a DVB
    /// service. Field values are copied out of the native struct, so the instance has no lifetime
    /// concerns. Obtained from <see cref="ProgramList"/>, <see cref="MediaPlayer.GetSelectedProgram"/>
    /// or <see cref="MediaPlayer.GetProgram"/>.
    /// </summary>
    public readonly struct Program
    {
        internal unsafe Program(libvlc_player_program_t* p)
        {
            GroupId = p->i_group_id;
            Name = ((IntPtr)p->psz_name).GetUtf8();
            IsSelected = p->b_selected.ToBool();
            IsScrambled = p->b_scrambled.ToBool();
        }

        /// <summary>Program group id (used by <see cref="MediaPlayer.SelectProgram"/>). <c>i_group_id</c>.</summary>
        public int GroupId { get; }
        /// <summary>Program name, or null. <c>psz_name</c>.</summary>
        public string? Name { get; }
        /// <summary>Whether the program is currently selected. <c>b_selected</c>.</summary>
        public bool IsSelected { get; }
        /// <summary>Whether the program is scrambled. <c>b_scrambled</c>.</summary>
        public bool IsScrambled { get; }
    }

    /// <summary>
    /// An owned, read-only list of <see cref="Program"/> (<c>libvlc_player_programlist_t</c>). Release
    /// it when done. Items are value snapshots, so they remain valid after the list is disposed.
    /// </summary>
    public class ProgramList : NativeReference, IReadOnlyList<Program>
    {
        /// <summary>Wraps an existing native handle.</summary>
        /// <param name="handle">Native <c>libvlc_player_programlist_t*</c>.</param>
        public ProgramList(IntPtr handle) : base(handle) { }

        /// <summary>Implicit conversion to the native <c>libvlc_player_programlist_t*</c> (null for a null list).</summary>
        public static unsafe implicit operator libvlc_player_programlist_t*(ProgramList list) =>
            list is null ? null : (libvlc_player_programlist_t*)list.NativeHandle;

        protected override unsafe void Release(IntPtr handle) =>
            libvlc_player_programlist_delete((libvlc_player_programlist_t*)handle);

        /// <summary>Number of programs. <c>libvlc_player_programlist_count</c>.</summary>
        public unsafe int Count => (int)libvlc_player_programlist_count(this).ToUInt32();

        /// <summary>The program at <paramref name="index"/> (a value snapshot). <c>libvlc_player_programlist_at</c>.</summary>
        public unsafe Program this[int index]
        {
            get
            {
                if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
                return new Program(libvlc_player_programlist_at(this, new UIntPtr((uint)index)));
            }
        }

        public IEnumerator<Program> GetEnumerator()
        {
            int count = Count;
            for (int i = 0; i < count; i++) yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
