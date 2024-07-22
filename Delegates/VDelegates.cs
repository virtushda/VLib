namespace VLib
{
    /// <summary>
    /// For use with binary searching.
    /// Add custom logic to perform a 'comparison' returning [-1, 0 or 1] for [less than, equal to, and greater than] the input object.
    /// </summary>
    public delegate int Compare<in T>(T obj);
}