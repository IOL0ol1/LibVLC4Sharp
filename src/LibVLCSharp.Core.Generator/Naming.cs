using System;
using System.Text;

namespace LibVLCSharp.Core.Generator
{
    /// <summary>
    /// Shared naming helpers for the alias generators (enums and delegates). Converts libvlc
    /// snake_case identifiers to C# PascalCase, fixing up proper-noun acronyms that libvlc spells
    /// as a single lowercase token.
    /// </summary>
    internal static class Naming
    {
        public const string AliasNamespace = "LibVLCSharp.Core";
        public const string InteropNamespace = "LibVLCSharp.Core.Interop";

        // Proper nouns / acronyms that naive PascalCase would mangle, because libvlc spells them as
        // a single lowercase token. Applied as whole-word fix-ups on the PascalCased result.
        // e.g. "abloop" (A-B playback loop) -> "ABLoop", not "Abloop".
        private static readonly (string From, string To)[] Acronyms =
        {
            ("Abloop", "ABLoop"),
        };

        /// <summary>
        /// PascalCase a snake_case value, preserving any intra-segment capitals (so libvlc's
        /// camelCase tokens like "makeCurrent" -&gt; "MakeCurrent"), then apply the acronym fix-ups.
        /// A leading digit is prefixed with '_' to stay a valid identifier.
        /// </summary>
        public static string Pascal(string value)
        {
            var parts = value.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var p in parts)
                sb.Append(char.ToUpperInvariant(p[0])).Append(p.Substring(1));
            var result = sb.ToString();
            foreach (var (from, to) in Acronyms)
                result = result.Replace(from, to);
            return result.Length > 0 && char.IsDigit(result[0]) ? "_" + result : result;
        }
    }
}
