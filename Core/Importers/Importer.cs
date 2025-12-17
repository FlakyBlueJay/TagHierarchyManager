using TagHierarchyManager.Models;

namespace TagHierarchyManager.Importers;

/// <summary>
///     An abstract class implementing an importer, converting a file in a specific format to a Dictionary of <see cref="ImportedTag"/>s.<br/>
///     The resulting Dictionary can then be used to fill a <see cref="TagDatabase"/> on creation.
/// </summary>
public abstract class Importer
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Importer" /> class.
    /// </summary>
    protected Importer()
    {
    }
    
    /// <summary>
    ///     Gets or sets the name of the format, for use in file dialogs.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string FormatName { get; protected init; } = string.Empty;

    /// <summary>
    ///     Gets all the contained text of a given file, then sends that string to <see cref="ProcessDataToDatabaseAsync" />.
    /// </summary>
    /// <param name="filePath">The location of the file to be imported.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation, returning a Dictionary of <see cref="ImportedTag"/> objects.</returns>
    public async Task<Dictionary<string, ImportedTag>> ImportFromFileAsync(string filePath)
    {
        string importedData = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        return await this.ProcessDataToDatabaseAsync(importedData).ConfigureAwait(false);
    }

    /// <summary>
    ///     Abstract method for inherited classes to implement the actual importing logic.
    /// </summary>
    /// <param name="importedData">The string containing the data to be imported.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    protected abstract Task<Dictionary<string, ImportedTag>> ProcessDataToDatabaseAsync(string importedData);
}