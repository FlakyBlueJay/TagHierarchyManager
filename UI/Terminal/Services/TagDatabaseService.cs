using TagHierarchyManager.Common;
using TagHierarchyManager.Exporters;
using TagHierarchyManager.Importers;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.TerminalUI.Views;
using Terminal.Gui.App;

namespace TagHierarchyManager.UI.TerminalUI.Services;

/// <summary>
///     Handles interaction between the <see cref="TagDatabase" /> and the UI.
/// </summary>
public static class TagDatabaseService
{
    /// <summary>
    ///     An event thrown when the database initialisation has started.
    /// </summary>
    public static event EventHandler DatabaseInitialising = delegate { };

    /// <summary>
    ///     An event thrown when the database export process has completed, storing a string with it pointing to the
    ///     export's file path.
    /// </summary>
    public static event EventHandler<string> ExportCompleted = delegate { };

    /// <summary>
    ///     Thrown when an exception has occurred while exporting, storing a string with it
    ///     specifying the error.
    /// </summary>
    public static event EventHandler<string> ExportError = delegate { };

    /// <summary>
    ///     Thrown when the database export process has started.
    /// </summary>
    public static event EventHandler ExportStarted = delegate { };

    /// <summary>
    ///     Thrown when an exception has occurred while processing data to import, storing a string with it
    ///     specifying the error.
    /// </summary>
    public static event EventHandler<string> ImportError = delegate { };

    /// <summary>
    ///     Thrown when the initialisation has completed.
    /// </summary>
    public static event EventHandler InitialisationComplete = delegate { };

    /// <summary>
    ///     Thrown when an exception has occurred while attempting to initialise the database, storing a string with it
    ///     specifying the error.
    /// </summary>
    public static event EventHandler<string> InitialisationError = delegate { };

    /// <summary>
    ///     Thrown when the search process has finished, storing with it the number of Tags found.
    /// </summary>
    public static event EventHandler<List<Tag>> SearchFinished = delegate { };

    /// <summary>
    ///     Thrown when a new Tag has been added, storing the newly created Tag object with it.
    /// </summary>
    public static event EventHandler<Tag> TagAdded = delegate { };

    /// <summary>
    ///     Thrown when a Tag has been deleted, storing the ID and name of the Tag with it.
    /// </summary>
    public static event EventHandler<(int, string)> TagDeleted = delegate { };

    /// <summary>
    ///     Thrown when a Tag has been saved, regardless of whether it was new or not, storing the saved Tag object
    ///     with it.
    /// </summary>
    public static event EventHandler<Tag> TagSaved = delegate { };
    
    // should this be generic and inside the Core?
    // not yet, we'll see when i get to adding the avalonia ui.

    /// <summary>
    ///     Throws a save dialog to the user, and starts the <see cref="TagDatabase" /> initialisation process.
    /// </summary>
    public static void CreateDatabase((string filePath, bool userAllowedOverwrite) newDatabase)
    {
        StartDatabaseInit(newDatabase.filePath, newDatabase.userAllowedOverwrite);
    }

    public static async Task DeleteTag(Tag tag)
    {
        string tagName = tag.Name;
        int tagId = tag.Id;
        if (Program.CurrentDatabase != null) await Program.CurrentDatabase.DeleteTag(tag.Id);
        TagDeleted.Invoke(null, (tagId, tagName));

    }

    /// <summary>
    ///     Throws a load dialog to the user, and starts the <see cref="TagDatabase" /> initialisation process.
    /// </summary>
    public static void PromptDatabaseLoad()
    {
        string? newDatabasePath = StandardDialogs.DatabaseOpenDialog();
        if (newDatabasePath is null) return;

        StartDatabaseInit(newDatabasePath);
    }

    public static void SearchTags(string query, TagDatabaseSearchMode mode, bool searchAliases)
    {
        if (Program.CurrentDatabase is null) throw new InvalidOperationException(ErrorMessages.DbNotInitialised);

        List<Tag> tags = searchAliases 
            ? Program.CurrentDatabase.SearchWithAliases(query, mode)
            : Program.CurrentDatabase.Search(query, mode);
        SearchFinished.Invoke(Program.CurrentDatabase, tags.OrderBy(t => t.Name).ToList());
    }

    /// <summary>
    ///     Starts the database initialisation.
    /// </summary>
    /// <param name="filePath">The file path of the database to create or load.</param>
    /// <param name="overwriteOnCreation">
    ///     Whether to overwrite the database file on filePath.<br />
    ///     Null implies database load - true/false implies database creation.
    /// </param>
    /// <param name="importedFilePath">Optional path to a file to import into the new database (used on creation).</param>
    public static void StartDatabaseInit(
        string filePath,
        bool? overwriteOnCreation = null,
        string? importedFilePath = null)
    {
        ClearMainWindowData();

        TagDatabase db = new();
        SubscribeToEvents(db);

        _ = Task.Run(async () =>
        {
            try
            {
                Dictionary<string, ImportedTag>? tagsToImport = null;
                if (overwriteOnCreation.HasValue)
                {
                    if (!string.IsNullOrEmpty(importedFilePath))
                        try
                        {
                            tagsToImport = await PickImporterFromFileExt(importedFilePath)
                                .ImportFromFileAsync(importedFilePath);
                        }
                        catch (Exception ex)
                        {
                            Application.Invoke(() => ImportError.Invoke(null, ex.Message));
                            return;
                        }

                    DatabaseInitialising.Invoke(null, EventArgs.Empty);
                    await db.CreateAsync(filePath, overwriteOnCreation.Value,
                        tagsToImport);
                }
                else
                {
                    DatabaseInitialising.Invoke(null, EventArgs.Empty);
                    await db.LoadAsync(filePath);
                }
            }
            catch (Exception ex)
            {
                InitialisationError.Invoke(db, ex.Message);
                UnsubscribeFromEvents(db);
            }
        });
    }

    public static void StartExportProcess()
    {
        if (Program.CurrentDatabase is null) return;
        (string filePath, bool userAllowedOverwrite)? saveLocation = StandardDialogs.ExportSaveDialog();
        if (saveLocation is null) return;
        _ = Task.Run(async () =>
        {
            ExportStarted.Invoke(null, EventArgs.Empty);
            await ExportToFile(saveLocation.Value);
        });
    }
    
    public static async Task WriteTagToDatabase(Tag tag)
    {
        bool newTag = tag.Id == 0;
        if (Program.CurrentDatabase != null) await Program.CurrentDatabase.WriteTagToDatabase(tag);
        
        if (newTag) TagAdded.Invoke(Program.CurrentDatabase, tag);
        TagSaved.Invoke(Program.CurrentDatabase, tag);
    }

    /// <summary>
    ///     Clears the data associated with the main window, prompting the user if unsaved changes were detected.
    /// </summary>
    /// <returns>True if the data has been cleared.</returns>
    private static void ClearMainWindowData()
    {
        if (Program.CurrentDatabase is null) return;
        UnsubscribeFromEvents(Program.CurrentDatabase);
        Program.CurrentDatabase.Close();
    }

    private static async Task ExportToFile((string filePath, bool userAllowedOverwrite) exportLocation)
    {
        if (Program.CurrentDatabase is null) return;
        IExporter exporter = PickExporterFromFileExt(exportLocation.filePath);
        try
        {
            string export = exporter.ExportDatabase(Program.CurrentDatabase);
            if (exportLocation.userAllowedOverwrite && File.Exists(exportLocation.filePath))
                File.Delete(exportLocation.filePath);
            await File.WriteAllTextAsync(exportLocation.filePath, export);
            ExportCompleted.Invoke(null, exportLocation.filePath);
        }
        catch (Exception ex)
        {
            ExportError.Invoke(exportLocation.filePath, ex.Message);
        }
    }

    private static void OnInitialisationComplete(object? sender, EventArgs e)
    {
        Application.Invoke(() =>
        {
            Program.Logger.Debug("[LoadingDialog.OnInitialised] Event has been caught");
            Program.Logger.Information("[LoadingDialog.OnInitialised] Sender: {@Sender}", sender);
            if (sender is not TagDatabase db) return;

            Program.CurrentDatabase = db;

            InitialisationComplete.Invoke(db, EventArgs.Empty);
        });
    }

    private static IExporter PickExporterFromFileExt(string path)
    {
        string fileExt = Path.GetExtension(path);

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (fileExt == FileTypes.MusicBeeTagHierarchyTemplate.FileExtension) return new MusicBeeTagHierarchyExporter();

        throw new NotSupportedException($"File extension '{fileExt}' is not supported.");
    }

    private static Importer PickImporterFromFileExt(string path)
    {
        string fileExt = Path.GetExtension(path);

        // if more importers are added, convert this to a switch statement based on file extension.
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (fileExt == FileTypes.MusicBeeTagHierarchyTemplate.FileExtension) return new MusicBeeTagHierarchyImporter();

        throw new NotSupportedException($"File extension '{fileExt}' is not supported.");
    }

    private static void SubscribeToEvents(TagDatabase db)
    {
        db.InitialisationComplete += OnInitialisationComplete;
    }

    private static void UnsubscribeFromEvents(TagDatabase db)
    {
        db.InitialisationComplete -= OnInitialisationComplete;
    }
}