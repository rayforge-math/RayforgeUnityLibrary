using System;

namespace Rayforge.Core.Rendering.Filtering
{
    /// <summary>
    /// Represents a parametrized 1D filter function.
    /// Acts as a lightweight function object (functor) with explicit parameters.
    /// </summary>
    /// <typeparam name="TParam">Filter-specific parameter type.</typeparam>
    public struct Filter<TParam>
    {
        /// <summary>
        /// Delegate used to compute a kernel weight at a given radius index.
        /// </summary>
        /// <param name="x">Distance from the kernel center.</param>
        /// <param name="param">Filter-specific parameter.</param>
        /// <returns>Computed kernel weight.</returns>
        public delegate float FilterFunction(int x, TParam param);

        private FilterFunction m_FilterFunc;
        private TParam m_Param;

        /// <summary>
        /// Parameter passed to the filter function during evaluation.
        /// </summary>
        public TParam Param
        {
            get => m_Param;
            set => m_Param = value;
        }

        /// <summary>
        /// Creates a new filter wrapper around the given function and parameter.
        /// </summary>
        /// <param name="function">The filter function to invoke. Must not be null.</param>
        /// <param name="param">Filter-specific parameter.</param>
        /// <exception cref="ArgumentNullException">Thrown if the function is null.</exception>
        public Filter(FilterFunction function, TParam param)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function), "Filter function cannot be null.");

            m_FilterFunc = function;
            m_Param = param;
        }

        /// <summary>
        /// Evaluates the filter at the given kernel index.
        /// </summary>
        /// <param name="x">Distance from the kernel center.</param>
        /// <returns>Computed kernel weight.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the filter function was not initialized.</exception>
        public float Invoke(int x)
        {
            if (m_FilterFunc == null)
                throw new InvalidOperationException("Cannot invoke filter: function is null.");

            return m_FilterFunc.Invoke(x, m_Param);
        }
    }
}