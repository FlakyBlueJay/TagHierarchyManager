namespace TagHierarchyManager.Models;

/// <summary>
///     An object representing a tag entry in a tag hierarchy database.
/// </summary>
public partial class Tag
{
    /// <summary>
    ///     Gets or sets a list of strings containing aliases (or "also known as"), saved in the database as semicolons.
    /// </summary>
    public List<string> Aliases { get; set; } = [];

    /// <summary>
    ///     Gets or sets he internal ID of a tag entry.<br /><br />
    ///     Can be zero when constructing a Tag to send to the database, but should be more than zero for tags retrieved
    ///     from the database.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the tag is top level.
    /// </summary>
    public required bool IsTopLevel { get; set; }

    /// <summary>
    ///     Gets or sets the user-facing name of the tag.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Gets or sets a string for any plain text associated with the tag to be used for notes.
    ///     This is mainly for internal use by the user.
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    // TODO ensure ParentIds is the authority on parent-child relationships in both the core + UI.
    /// <summary>
    ///     Gets or sets a list of the tag entry's parent IDs for interaction with the database.
    /// </summary>
    public List<int> ParentIds { get; set; } = [];

    /// <summary>
    ///     Gets or sets a list of the tag entry's parent names, stored for saving new parents in a user interface.
    /// </summary>
    public List<string> Parents { get; set; } = [];

    /// <summary>
    ///     Gets or sets a list of strings, listing tag bindings associated with the current tag entry.
    ///     Internally it will be stored as a semicolons (e.g. genre; style)<br />
    ///     A tag can have no bindings, if the user wants to use it as a category and not a tag in itself.
    /// </summary>
    public List<string> TagBindings { get; set; } = [];

    /// <inheritdoc />
    public override string ToString()
    {
        return this.Name + (this.TagBindings.Count > 0 ? $" ({string.Join("; ", this.TagBindings)})" : string.Empty);
    }

    /// <summary>
    ///     Performs two validation checks: if the tag is not top level and the Parents and ParentNames properties are
    ///     empty (basically making the tag "orphaned"), and if an attempt is made to make a tag its own parent.
    /// </summary>
    /// <returns>
    ///     true if the TagEntry was validated to not be orphaned (not top level + no parents) and not be
    ///     self-parenting.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when both Parents and ParentNames are empty.</exception>
    public bool Validate()
    {
        if (!this.IsTopLevel && this.ParentIds.Count == 0 && this.Parents.Count == 0)
            throw new InvalidOperationException(ErrorMessages.OrphanTagAttempt(this.Name));

        if (this.Parents.Contains(this.Name) || this.ParentIds.Contains(this.Id))
            throw new InvalidOperationException(ErrorMessages.MakingSelfParentAttempt(this.Name));

        return true;
    }
}