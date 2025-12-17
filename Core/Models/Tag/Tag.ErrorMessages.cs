namespace TagHierarchyManager.Models;

public partial class Tag
{
    /// <summary>
    ///     Error messages associated with the Tag object.
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>
        ///     Constructs a string indicating an attempt to make a tag a parent of itself.
        /// </summary>
        /// <param name="name">The name of the offending Tag object.</param>
        /// <returns>
        ///     An error message string indicating that the tag has itself in Parents (id) or ParentNames (name).
        /// </returns>
        public static string MakingSelfParentAttempt(string name)
        {
            return $"Tag '{name}' has itself in Parents or ParentNames, which is invalid.";
        }

        /// <summary>
        ///     Constructs a string indicating that a specified tag is an orphan.
        /// </summary>
        /// <param name="name">The name of the offending Tag object.</param>
        /// <returns>A string indicating that the tag has no parents specified and can't be top level.</returns>
        public static string OrphanTagAttempt(string name)
        {
            return $"Tag '{name}' has no parents specified, so cannot be non-top-level.";
        }
    }
}