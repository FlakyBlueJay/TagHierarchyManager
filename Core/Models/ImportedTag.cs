namespace TagHierarchyManager.Models;

/// <summary>
///     This class represents a tag that has been imported from an external source.
///     This is intended for intermediate representation before being added to the TagDatabase.
/// </summary>
public class ImportedTag
{
    /// <summary>
    ///     Gets or sets a list of strings containing aliases/"also known as". Internally, this is saved as semicolons.
    /// </summary>
    // disable resharper warnings since it can be used with other formats that use aliases.
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper disable once CollectionNeverUpdated.Global
    public HashSet<string> Aliases { get; set; } = [];

    /// <summary>
    ///     Gets or sets a value indicating whether the tag is top level.
    /// </summary>
    public required bool IsTopLevel { get; set; }

    /// <summary>
    ///     Gets or sets the user-facing name of the tag.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets or sets a string for any plain text specified. This will not be shown on exports and is intended for internal
    ///     use.
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a list of the tag entry's parent names, stored for saving new parents in a user interface.
    /// </summary>
    public HashSet<string> Parents { get; set; } = [];

    /// <summary>
    ///     Gets or sets a list of strings, listing tag bindings associated with the current tag entry.
    ///     Internally it will be stored as a semicolons (e.g. genre; style)<br />
    ///     A tag can have no bindings, if the user wants to use it as a category and not a tag in itself.
    /// </summary>
    public HashSet<string> TagBindings { get; set; } = [];
}