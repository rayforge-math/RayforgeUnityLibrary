namespace Rayforge.Rendering.Filtering
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
        public Filter(FilterFunction function, TParam param)
        {
            m_FilterFunc = function;
            m_Param = param;
        }

        /// <summary>
        /// Evaluates the filter at the given kernel index.
        /// </summary>
        public float Invoke(int x)
            => m_FilterFunc.Invoke(x, m_Param);
    }
}