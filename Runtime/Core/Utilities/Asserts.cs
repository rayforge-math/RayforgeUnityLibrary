using System.Runtime.CompilerServices;
using UnityEngine;

namespace Rayforge.CustomUtility.RuntimeCheck
{
    public static class Asserts
    {
        /// <summary>
        /// Delegate defining a validation function that returns true when a value passes the expected condition.
        /// </summary>
        /// <typeparam name="Tval">The value type to validate.</typeparam>
        /// <param name="value">The value being evaluated.</param>
        /// <returns>True if the value is valid, otherwise false.</returns>
        public delegate bool AssertDelegate<Tval>(Tval value);

        /// <summary>
        /// Validates a value using the provided assertion function.
        /// If validation fails:
        /// - In the Unity Editor, an error is logged with file and line context.
        /// - In a player build, an exception is thrown.
        /// </summary>
        /// <typeparam name="Tval">The type of the value being validated.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="assertFunc">A delegate returning true if the value is valid.</param>
        /// <param name="error">A short descriptive message explaining the validation failure.</param>
        /// <param name="file">Compiler-supplied path to the calling source file. Do not provide manually.</param>
        /// <param name="line">Compiler-supplied line number where the validation was called. Do not provide manually.</param>
        /// <param name="member">Compiler-supplied calling member name. Do not provide manually.</param>
        public static void Validate<Tval>(
            Tval value,
            AssertDelegate<Tval> assertFunc,
            string error,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
#if UNITY_EDITOR
            if (!assertFunc(value))
                Debug.LogError($"{error}\n{file}:{line} ({member})");
#else
            if (!assertFunc(value))
                throw new Exception($"{error}\n{file}:{line} ({member})");
#endif
        }

        /// <summary>
        /// Validates that a delegate reference is not null.
        /// Useful for ensuring callbacks and event hooks are assigned.
        /// </summary>
        /// <typeparam name="Tdelegate">The delegate type being validated.</typeparam>
        /// <param name="func">The delegate instance to check.</param>
        public static void ValidateDelegate<Tdelegate>(Tdelegate func)
        {
            const string error = "func must be a valid delegate";
            Validate(func, f => f != null, error);
        }
    }
}