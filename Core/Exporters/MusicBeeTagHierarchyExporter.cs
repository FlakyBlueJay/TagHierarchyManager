using System.Text;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.Exporters;

/// <summary>
///     An IExporter class implementing the export of a MusicBee tag hierarchy template.
/// </summary>
public class MusicBeeTagHierarchyExporter : IExporter
{
    /// <summary>
    ///     Exports the <see cref="TagDatabase" /> to a string consisting of a tag hierarchy template that can be used in
    ///     MusicBee.
    /// </summary>
    /// <param name="db">The <see cref="TagDatabase" /> to export.</param>
    /// <returns>A string containing the tag hierarchy template.</returns>
    public string ExportDatabase(TagDatabase db)
    {
        ArgumentNullException.ThrowIfNull(db);

        List<Tag> topLevelTags = db.Tags.Where(tag => tag.IsTopLevel).OrderBy(tag => tag.Name).ToList();
        topLevelTags = topLevelTags.OrderBy(tag => tag.Name).ToList();
        StringBuilder currentString = new();
        foreach (Tag topLevelTag in topLevelTags)
            ProcessRecursively(currentString, db, topLevelTag, string.Empty);

        return currentString.ToString();
    }

    private static void ProcessRecursively(StringBuilder currentString, TagDatabase db, Tag currentTag,
        string indent = "")
    {
        List<Tag> tagChildren = db.GetTagChildren(currentTag.Id).OrderBy(tag => tag.Name).ToList();
        if (tagChildren.Count > 0)
        {
            currentString.AppendLine(indent + currentTag.Name);
            indent += "    ";

            if (currentTag.TagBindings.Count > 0) ProcessTagBindings(currentTag, indent, currentString);
        }
        else
        {
            if (currentTag.TagBindings.Count == 0)
                currentString.AppendLine(indent + currentTag.Name);
            else
                ProcessTagBindings(currentTag, indent, currentString);
        }

        foreach (Tag childTag in tagChildren)
            ProcessRecursively(currentString, db, childTag, indent);
    }

    private static void ProcessTagBindings(Tag currentTag, string indent, StringBuilder builder)
    {
        foreach (string line in
                 currentTag.TagBindings.Select(tagBinding => indent + currentTag.Name + $"::{tagBinding}"))
            builder.AppendLine(line);
    }
}