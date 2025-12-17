using TagHierarchyManager.Common;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TagHierarchyManager.UI.TerminalUI.Views;

/// <summary>
///     A class storing standard dialogs.
/// </summary>
internal static class StandardDialogs
{
    /// <summary>
    ///     Shows a <see cref="MessageBox" /> containing the name, copyright, license and GitHub link.
    /// </summary>
    public static void AboutMessageBox()
    {
        const string message = """
                               Tag Hierarchy Manager
                               (c) Flaky 2025-

                               Licensed under MIT

                               https://github.com/FlakyBlueJay/TagHierarchyManager

                               """;
        MessageBox.Query("About Tag Hierarchy Manager", message, "OK");
    }

    /// <summary>
    ///     Calls <see cref="UnsavedChangesMessageBox" /> and cancels <see cref="Toplevel" /> Closing event if the user chooses
    ///     to cancel.
    /// </summary>
    /// <param name="e">The <see cref="ToplevelClosingEventArgs" /> object, for cancelling if the user chooses to cancel.</param>
    public static void CallUnsavedChangesMessageBox(ToplevelClosingEventArgs e)
    {
        int result = UnsavedChangesMessageBox();
        if (result == 1) e.Cancel = true;
    }

    /// <summary>
    ///     Opens an <see cref="OpenDialog" /> with the purpose of opening a database.
    /// </summary>
    /// <returns>The result of a <see cref="SingleFileOpenDialog" />, the path of the chosen .thdb file.</returns>
    public static string? DatabaseOpenDialog()
    {
        List<IAllowedType> allowedTypes =
        [
            new AllowedType(FileTypes.TagDatabase.Name, FileTypes.TagDatabase.FileExtension),
        ];

        return SingleFileOpenDialog("Open a tag hierarchy database...", allowedTypes);
    }

    /// <summary>
    ///     Runs a <see cref="SaveDialog" /> for the purpose of creating a new database.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTuple" /> created with the file path and choice of whether the user approved
    ///     overwriting the file from the resulting <see cref="SaveDialog" />.
    /// </returns>
    public static (string filePath, bool userAllowedOverwrite)? ExportSaveDialog()
    {
        List<IAllowedType> allowedTypes = [];
        FileTypes.AllNonDatabaseFileTypes.ForEach(fileType =>
            allowedTypes.Add(new AllowedType(fileType.Name, fileType.FileExtension)));
        return RunSaveDialog(allowedTypes);
    }

    /// <summary>
    ///     Opens an <see cref="OpenDialog" /> with the purpose of choosing a file to import.
    /// </summary>
    /// <returns>The result of a <see cref="SingleFileOpenDialog" />, the path of the chosen file to import.</returns>
    public static string? ImportOpenDialog()
    {
        List<IAllowedType> allowedTypes = ConvertFileTypesToAllowedTypes();
        return SingleFileOpenDialog("Choose file to import...", allowedTypes);
    }

    /// <summary>
    ///     Runs a <see cref="SaveDialog" /> for the purpose of creating a new database.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTuple" /> created with the file path and choice of whether the user approved
    ///     overwriting the file from the resulting <see cref="SaveDialog" />.
    /// </returns>
    public static (string fileName, bool userAllowedOverwrite)? NewDatabaseSaveDialog()
    {
        List<IAllowedType> allowedTypes =
        [
            new AllowedType(FileTypes.TagDatabase.Name, FileTypes.TagDatabase.FileExtension),
        ];

        return RunSaveDialog(allowedTypes);
    }

    private static List<IAllowedType> ConvertFileTypesToAllowedTypes()
    {
        return FileTypes.AllNonDatabaseFileTypes.Select(IAllowedType (fileType) =>
                new AllowedType(fileType.Name, fileType.FileExtension))
            .ToList();
    }

    private static int OverwriteMessageBox(string path)
    {
        return MessageBox.Query(
            "Overwrite existing file?",
            $"A file already exists at\n{path}\n\nAre you sure you want to overwrite this file?",
            1,
            "Yes",
            "No");
    }

    private static (string filePath, bool userAllowedOverwrite)? RunSaveDialog(List<IAllowedType> allowedTypes,
        string title = "Save as...")
    {
        SaveDialog exportDialog = new()
        {
            AllowedTypes = allowedTypes,
            Title = title,
        };
        bool overwrite = false;
        exportDialog.FilesSelected += (_, e) =>
        {
            if (!File.Exists(e.Dialog.Path)) return;
            int result = OverwriteMessageBox(e.Dialog.Path);
            if (result == 0)
                overwrite = true;
            else
                e.Cancel = true;
        };
        Application.Run(exportDialog);
        return !exportDialog.Canceled ? (exportDialog.FileName, overwrite) : null;
    }

    private static string? SingleFileOpenDialog(string title, List<IAllowedType> allowedTypes)
    {
        OpenDialog openDialog = new()
        {
            OpenMode = OpenMode.File,
            AllowsMultipleSelection = false,
            AllowedTypes = allowedTypes,
            Title = title,
            Width = Dim.Fill(),
        };
        Application.Run(openDialog);
        return openDialog.FilePaths.Any() ? openDialog.FilePaths[0] : null;
    }

    private static int UnsavedChangesMessageBox()
    {
        return MessageBox.Query(
            "Are you sure?",
            "You have unsaved changes. Are you sure you want to leave?",
            1,
            "Yes",
            "No");
    }
}