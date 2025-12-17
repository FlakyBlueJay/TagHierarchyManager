using TagHierarchyManager.Models;

namespace TagHierarchyManager.Exporters;

/// <summary>
///     A base interface for implementing exporter classes.
/// </summary>
public interface IExporter
{
    /// <summary>
    ///     Exports the tag hierarchy to a string format.
    /// </summary>
    /// <param name="db">The <see cref="TagDatabase" /> to export.</param>
    /// <returns>A string representing the exported output.</returns>
    string ExportDatabase(TagDatabase db);
}