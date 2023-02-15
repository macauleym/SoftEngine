namespace SoftEngine.Core.Extensions
{
    public enum FloatComparison
    { Greater
    , Less
    , Equal
    , NotEqual
    }
    
    public static class FloatExtensions
    {
        public static bool CompareWith(this float a, float b, FloatComparison comparison, float tolerance = 0) =>
            comparison switch
            {
                FloatComparison.Greater  => a > b,
                FloatComparison.Less     => a < b,
                FloatComparison.Equal    => Math.Abs(a - b) < tolerance,
                FloatComparison.NotEqual => Math.Abs(a - b) > tolerance,
                _                        => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null)
            };
        
        /// <summary>
        /// Clamp a value to keep them between a min a max value.
        /// </summary>
        /// <param name="value">To be clamped.</param>
        /// <param name="min">Minimum value. Defaults to 0.</param>
        /// <param name="max">Maximum value. Defaults to 1.</param>
        /// <returns></returns>
        public static float Clamp01(this float value, float min = 0, float max = 1) =>
            Math.Max(min, Math.Min(value, max));
        
        /// <summary>
        /// Interpolate between 2 values.
        /// </summary>
        /// <param name="start">Start point.</param>
        /// <param name="end">End point.</param>
        /// <param name="gradiant">Gradiant % between the start and end.</param>
        /// <returns></returns>
        public static float InterpolateTo(this float start, float end, float gradiant) =>
            start + (end - start) * gradiant.Clamp01();
    }
}
