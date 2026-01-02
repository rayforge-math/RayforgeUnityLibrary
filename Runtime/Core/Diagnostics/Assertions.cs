using System.Runtime.CompilerServices;
using UnityEngine;

namespace Rayforge.Diagnostics
{
    /// <summary>
    /// Provides editor/development-only runtime assertions for validating values, objects, and delegates.
    /// All calls are stripped in non-development builds if UNITY_EDITOR or DEVELOPMENT_BUILD is not defined.
    /// </summary>
    public static class Assertions
    {
        /// <summary>
        /// Basic boolean assertion.
        /// Logs an error in the Unity editor or throws an exception in development builds if the condition is false.
        /// </summary>
        /// <param name="condition">The boolean condition to validate. Must be true to pass.</param>
        /// <param name="error">Error message to display if the assertion fails.</param>
        /// <param name="file">Compiler-supplied source file path. Supplied automatically.</param>
        /// <param name="line">Compiler-supplied line number. Supplied automatically.</param>
        /// <param name="member">Compiler-supplied calling member name. Supplied automatically.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Assert(
            bool condition,
            string error,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
#if UNITY_EDITOR
            if (!condition)
                Debug.LogError($"{error}\n{file}:{line} ({member})");
#else
            if (!condition)
                throw new System.Exception($"{error}\n{file}:{line} ({member})");
#endif
        }

        /// <summary>
        /// Validates that an object reference is not null.
        /// </summary>
        /// <typeparam name="T">The type of the object being validated.</typeparam>
        /// <param name="obj">The object to validate. Must not be null.</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void NotNull<T>(T obj, string error = null)
        {
            Assert(obj != null, error ?? "Object must not be null.");
        }

        /// <summary>
        /// Validates that the provided object is of the expected type <typeparamref name="TExpected"/>.
        /// Throws or logs an error if the object is not of that type.
        /// </summary>
        /// <typeparam name="TExpected">The type that <paramref name="obj"/> is expected to be.</typeparam>
        /// <param name="obj">The object to validate. Must be of type <typeparamref name="TExpected"/>.</param>
        /// <param name="error">Optional custom error message. Defaults to a message indicating the required type.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void IsTypeOf<TExpected>(object obj, string error = null)
        {
            Assert(obj is TExpected, error ?? $"Object must be of type {typeof(TExpected).Name}.");
        }

        /// <summary>
        /// Validates that an integer value is greater than zero.
        /// </summary>
        /// <param name="value">The integer value to check. Must be > 0.</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void GreaterThanZero(int value, string error = null)
        {
            Assert(value > 0, error ?? "Value must be greater than 0.");
        }

        /// <summary>
        /// Validates that a floating-point value is greater than zero.
        /// </summary>
        /// <param name="value">The float value to check. Must be > 0.</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void GreaterThanZero(float value, string error = null)
        {
            Assert(value > 0f, error ?? "Value must be greater than 0.");
        }

        /// <summary>
        /// Validates that an integer value is greater than or equal to zero.
        /// </summary>
        /// <param name="value">The integer value to check. Must be > 0.</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void AtLeastZero(int value, string error = null)
        {
            Assert(value >= 0, error ?? "Value must be greater than or equal to 0.");
        }

        /// <summary>
        /// Validates that a floating-point value is greater than or equal to zero.
        /// </summary>
        /// <param name="value">The float value to check. Must be > 0.</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void AtLeastZero(float value, string error = null)
        {
            Assert(value > 0f || Mathf.Approximately(value, 0f), error ?? "Value must be greater than or equal to 0.");
        }

        /// <summary>
        /// Validates that an integer value is not zero.
        /// </summary>
        /// <param name="value">The integer value to check. Must not be zero.</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void NotZero(int value, string error = null)
        {
            Assert(value != 0, error ?? "Value must not be zero.");
        }

        /// <summary>
        /// Validates that a floating-point value is not zero.
        /// </summary>
        /// <param name="value">The float value to check. Must not be zero.</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void NotZero(float value, string error = null)
        {
            Assert(value != 0f, error ?? "Value must not be zero.");
        }

        /// <summary>
        /// Validates that a numeric value lies within a specified inclusive range.
        /// </summary>
        /// <param name="value">The integer value to check.</param>
        /// <param name="min">Minimum allowed value (inclusive).</param>
        /// <param name="max">Maximum allowed value (inclusive).</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void InRange(int value, int min, int max, string error = null)
        {
            Assert(value >= min && value <= max, error ?? $"Value must be in range [{min}, {max}].");
        }

        /// <summary>
        /// Validates that a numeric value lies within a specified inclusive range.
        /// </summary>
        /// <param name="value">The float value to check.</param>
        /// <param name="min">Minimum allowed value (inclusive).</param>
        /// <param name="max">Maximum allowed value (inclusive).</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void InRange(float value, float min, float max, string error = null)
        {
            Assert(value >= min && value <= max, error ?? $"Value must be in range [{min}, {max}].");
        }

        /// <summary>
        /// Validates that a boolean value is true.
        /// </summary>
        /// <param name="value">The boolean value to check. Must be true.</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void IsTrue(bool value, string error = null)
        {
            Assert(value, error ?? "Value must be true.");
        }

        /// <summary>
        /// Validates that a boolean value is false.
        /// </summary>
        /// <param name="value">The boolean value to check. Must be false.</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void IsFalse(bool value, string error = null)
        {
            Assert(!value, error ?? "Value must be false.");
        }

        /// <summary>
        /// Validates that two values are equal.
        /// </summary>
        /// <typeparam name="T">The type of the values to compare.</typeparam>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value. Must be equal to the first.</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void AreEqual<T>(T a, T b, string error = null)
        {
            Assert(Equals(a, b), error ?? $"Values must be equal: {a} != {b}");
        }

        /// <summary>
        /// Validates that two values are not equal.
        /// </summary>
        /// <typeparam name="T">The type of the values to compare.</typeparam>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value. Must not be equal to the first.</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void AreNotEqual<T>(T a, T b, string error = null)
        {
            Assert(!Equals(a, b), error ?? $"Values must not be equal: {a} == {b}");
        }

        /// <summary>
        /// Validates that a delegate reference is not null.
        /// </summary>
        /// <typeparam name="TDelegate">Type of the delegate.</typeparam>
        /// <param name="func">The delegate to check. Must not be null.</param>
        /// <param name="error">Optional custom error message.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void NotNullDelegate<TDelegate>(TDelegate func, string error = null)
        {
            Assert(func != null, error ?? "Delegate must not be null.");
        }
    }
}