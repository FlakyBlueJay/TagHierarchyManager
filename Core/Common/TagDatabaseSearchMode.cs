using TagHierarchyManager.Models;

namespace TagHierarchyManager.Common;

/// <summary>
///     An enum representing the different search modes for the <see cref="TagDatabase"/>.
/// </summary>
public enum TagDatabaseSearchMode
{
    /// <summary>
    ///     Represents a "fuzzy" search, finding any tag whose name matches the relevant query in no matter where it is
    ///     matched.
    /// </summary>
    Fuzzy = 0,

    /// <summary>
    ///     Represents a search mode where the query must match at the beginning of the name.
    /// </summary>
    StartsWith = 1,

    /// <summary>
    ///     Represents a search mode where the query must match at the end of the name.
    /// </summary>
    EndsWith = 2,

    /// <summary>
    ///     Represents a search mode where it must match the exact query.
    /// </summary>
    ExactMatch = 3,
}