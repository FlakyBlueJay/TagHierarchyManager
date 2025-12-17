using TagHierarchyManager.Common;
using TagHierarchyManager.Utilities;

namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    /// <summary>
    /// Performs a search on the Tags list, the results dependent on the specified search mode.
    /// </summary>
    /// <param name="searchQuery">The string to search the tag names for.</param>
    /// <param name="mode">The mode to search for, see <see cref="TagDatabaseSearchMode"/>.</param>
    /// <returns>A List of Tags representing the search results.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the mode is out of the valid range specified by TagDatabaseSearchMode.
    /// </exception>
    public List<Tag> Search(string searchQuery, TagDatabaseSearchMode mode)
    {
        searchQuery = StringNormaliser.FormatStringForSearch(searchQuery.Trim());
        List<Tag> tags = mode switch
        {
            TagDatabaseSearchMode.Fuzzy => this.Tags
                .Where(tag => StringNormaliser.FormatStringForSearch(tag.Name).Contains(searchQuery))
                .ToList(),
            TagDatabaseSearchMode.StartsWith => this.Tags
                .Where(tag => StringNormaliser.FormatStringForSearch(tag.Name).StartsWith(searchQuery))
                .ToList(),
            TagDatabaseSearchMode.EndsWith => this.Tags
                .Where(tag => StringNormaliser.FormatStringForSearch(tag.Name).EndsWith(searchQuery))
                .ToList(),
            TagDatabaseSearchMode.ExactMatch => this.Tags
                .Where(tag => StringNormaliser.FormatStringForSearch(tag.Name) == searchQuery)
                .ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };

        return tags;
    }
    
    /// <summary>
    /// Performs a search on the Tags list, the results dependent on the specified search mode.<br/>
    /// Searches tag aliases, as well as tag names.
    /// </summary>
    /// <param name="searchQuery">The string to search the tag names and aliases for.</param>
    /// <param name="mode">The mode to search for, see <see cref="TagDatabaseSearchMode"/>.</param>
    /// <returns>A List of Tags representing the search results.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the mode is out of the valid range specified by TagDatabaseSearchMode.
    /// </exception>
    public List<Tag> SearchWithAliases(string searchQuery, TagDatabaseSearchMode mode)
    {
        searchQuery = StringNormaliser.FormatStringForSearch(searchQuery.Trim().ToLowerInvariant());
        List<Tag> tags = mode switch
        {
            TagDatabaseSearchMode.Fuzzy => this.Tags.Where(tag =>
                    StringNormaliser.FormatStringForSearch(tag.Name).Contains(searchQuery) || tag.Aliases.Any(alias =>
                        StringNormaliser.FormatStringForSearch(alias).Contains(searchQuery)))
                .ToList(),
            TagDatabaseSearchMode.StartsWith => this.Tags.Where(tag =>
                    StringNormaliser.FormatStringForSearch(tag.Name).StartsWith(searchQuery) || tag.Aliases.Any(alias =>
                        StringNormaliser.FormatStringForSearch(alias).StartsWith(searchQuery)))
                .ToList(),
            TagDatabaseSearchMode.EndsWith => this.Tags.Where(tag =>
                    StringNormaliser.FormatStringForSearch(tag.Name).EndsWith(searchQuery) || tag.Aliases.Any(alias =>
                        StringNormaliser.FormatStringForSearch(alias).EndsWith(searchQuery)))
                .ToList(),
            TagDatabaseSearchMode.ExactMatch => this.Tags.Where(tag =>
                    StringNormaliser.FormatStringForSearch(tag.Name) == searchQuery || tag.Aliases.Any(alias =>
                        StringNormaliser.FormatStringForSearch(alias) == searchQuery))
                .ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };

        return tags;
    }
}