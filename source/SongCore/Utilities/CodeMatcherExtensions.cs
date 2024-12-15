using System;
using HarmonyLib;
using IPA.Utilities;

namespace SongCore.Utilities
{
    public static class CodeMatcherExtensions
    {
        private static readonly FieldAccessor<CodeMatcher, string>.Accessor LastErrorAccessor =
            FieldAccessor<CodeMatcher, string>.GetAccessor("lastError");

        /// <summary>Prints the list of instructions of this code matcher instance.</summary>
        /// <param name="codeMatcher">The code matcher instance.</param>
        /// <returns>The code matcher instance.</returns>
        public static CodeMatcher PrintInstructions(this CodeMatcher codeMatcher)
        {
            var instructions = codeMatcher.Instructions();
            for (var i = 0; i < instructions.Count; i++)
            {
                Plugin.Log.Info($"\t {i} {instructions[i]}");
            }

            return codeMatcher;
        }

        /// <summary>Throws an exception if current state is invalid (position out of bounds/last match failed).</summary>
        /// <param name="codeMatcher">The code matcher instance.</param>
        /// <param name="explanation">Optional explanation of where/why the exception was thrown that will be added to the exception message.</param>
        /// <exception cref="InvalidOperationException">Current state is invalid.</exception>
        /// <returns>The code matcher instance.</returns>
        public static CodeMatcher ThrowIfInvalid(this CodeMatcher codeMatcher, string? explanation = null)
        {
            if (codeMatcher.IsInvalid)
            {
                var lastError = LastErrorAccessor(ref codeMatcher);
                var errMsg = lastError;
                if (!string.IsNullOrWhiteSpace(explanation))
                {
                    errMsg = $"{explanation} - Current state is invalid. Details: {lastError}";
                }

                throw new InvalidOperationException(errMsg);
            }

            return codeMatcher;
        }
    }
}
