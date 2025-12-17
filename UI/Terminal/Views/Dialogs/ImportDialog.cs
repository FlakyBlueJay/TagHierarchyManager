using TagHierarchyManager.Models;
using TagHierarchyManager.UI.TerminalUI.Services;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TagHierarchyManager.UI.TerminalUI.Views;

/// <summary>
///     A <see cref="UserInputDialog" />  for importing MusicBee tag hierarchy files.
/// </summary>
internal class ImportDialog : UserInputDialog
{
    private (string filePath, bool userAllowedOverwrite)? chosenDatabaseLocation;
    private Label errorLabel = null!;
    private View importingView = null!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ImportDialog" /> class.
    /// </summary>
    public ImportDialog()
    {
        this.InitialiseUI();
    }

    /// <summary>
    ///     Gets the  object representing the current database to save to.
    /// </summary>
    internal (string filePath, bool userAllowedOverwrite)? CurrentDatabase => this.chosenDatabaseLocation;

    /// <summary>
    ///     Gets the <see cref="TextField" /> containing the file path to where the new <see cref="TagDatabase" /> should be
    ///     saved.
    /// </summary>
    private TextField DatabaseField { get; set; } = null!;

    /// <summary>
    ///     Gets the <see cref="TextField" /> containing the file path to the target file for import.
    /// </summary>
    private TextField ImportedFileField { get; set; } = null!;

    private void databaseBrowseButton_OnAccepting(object? sender, CommandEventArgs e)
    {
        (string filePath, bool userAllowedOverwrite)? dbFile = StandardDialogs.NewDatabaseSaveDialog();
        if (dbFile.HasValue)
        {
            this.chosenDatabaseLocation = dbFile;
            this.DatabaseField.Text = dbFile.Value.filePath;
            this.DatabaseField.CursorPosition = dbFile.Value.filePath.Length;

            if (!string.IsNullOrEmpty(this.ImportedFileField.Text)) this.OkButton.Enabled = true;
        }

        e.Handled = true;
    }

    private void InitialiseUI()
    {
        this.Title = "Import file";
        this.Arrangement = ViewArrangement.Movable;
        this.Width = 70;
        this.Height = 12;

        Label tagHierarchyLabel = new()
        {
            X = 0,
            Y = 0,
            Title = "File to import:",
        };

        const string browseTitle = "Browse";

        this.ImportedFileField = new TextField
        {
            Y = Pos.Bottom(tagHierarchyLabel),
            Width = Dim.Fill() ! - (browseTitle.Length + 5),
            CanFocus = false,
            CursorVisibility = CursorVisibility.Invisible,
        };

        Button importFileBrowseButton = new()
        {
            X = Pos.Right(this.ImportedFileField),
            Y = Pos.Bottom(tagHierarchyLabel),
            Width = Dim.Fill(),
            Title = browseTitle,
        };
        importFileBrowseButton.Accepting += this.OnImportFileBrowseClick;

        Label databaseLabel = new()
        {
            X = 0,
            Y = Pos.Bottom(this.ImportedFileField) + 1,
            Title = "Database to save to:",
        };

        this.DatabaseField = new TextField
        {
            Y = Pos.Bottom(databaseLabel),
            Width = Dim.Fill() ! - (browseTitle.Length + 5),
            CanFocus = false,
            CursorVisibility = CursorVisibility.Invisible,
        };

        Button databaseBrowseButton = new()
        {
            X = Pos.Right(this.DatabaseField),
            Y = Pos.Bottom(databaseLabel),
            Width = Dim.Fill(),
            Title = browseTitle,
        };
        databaseBrowseButton.Accepting += this.databaseBrowseButton_OnAccepting;

        this.importingView = new View
        {
            X = 0,
            Y = Pos.Bottom(this.DatabaseField) + 1,
            Width = Dim.Fill(),
            Height = 1,
        };

        this.errorLabel = new Label
        {
            X = 0,
            Y = Pos.Bottom(this.DatabaseField) + 1,
            Width = Dim.Fill(),
            Visible = false,
            Text = "Import failed.",
        };

        SpinnerView spinner = new()
        {
            AutoSpin = true,
            Style = new SpinnerStyle.Dots(),
            X = 0,
        };

        Label importingLabel = new()
        {
            Title = "Importing...",
            Width = Dim.Auto(),
            X = Pos.Right(spinner) + 1,
        };

        this.OkButton.Accepting += this.OkButton_OnAccepting;
        this.OkButton.Enabled = false;

        this.Add(tagHierarchyLabel, this.ImportedFileField, importFileBrowseButton, databaseLabel,
            this.DatabaseField, databaseBrowseButton, this.importingView, this.errorLabel);
        this.importingView.Add(spinner, importingLabel);
        this.importingView.Visible = false;
        TagDatabaseService.ImportError += this.TagDatabaseService_OnImportError;
        TagDatabaseService.InitialisationComplete += this.TagDatabaseService_OnInitialisationComplete;
        this.Closing += this.OnClosing;
    }

    private void OkButton_OnAccepting(object? sender, CommandEventArgs e)
    {
        if (this.chosenDatabaseLocation is null ||
            string.IsNullOrEmpty(this.ImportedFileField.Text))
        {
            MessageBox.ErrorQuery(
                "Error",
                "One or more files were missing for the import process.\nPlease choose a valid file to import and/or a location for the tag database, and try again.",
                "OK");
        }
        else
        {
            foreach (Button button in this.Buttons) button.Enabled = false;
            this.errorLabel.Visible = false;
            this.importingView.Visible = true;
            TagDatabaseService.StartDatabaseInit(
                this.chosenDatabaseLocation.Value.filePath,
                this.chosenDatabaseLocation.Value.userAllowedOverwrite,
                this.ImportedFileField.Text);
        }

        e.Handled = true;
    }

    private void OnClosing(object? sender, ToplevelClosingEventArgs e)
    {
        if (this.importingView.Visible)
        {
            e.Cancel = true;
            return;
        }

        this.importingView.Visible = false;
        this.errorLabel.Visible = false;
        TagDatabaseService.ImportError -= this.TagDatabaseService_OnImportError;
        TagDatabaseService.InitialisationComplete -= this.TagDatabaseService_OnInitialisationComplete;

        this.Closing -= this.OnClosing;
    }

    private void OnImportFileBrowseClick(object? sender, CommandEventArgs e)
    {
        string? tagHierarchyFilePath = StandardDialogs.ImportOpenDialog();
        if (tagHierarchyFilePath is not null)
        {
            this.ImportedFileField.Text = tagHierarchyFilePath;
            this.ImportedFileField.CursorPosition = tagHierarchyFilePath.Length;

            if (this.chosenDatabaseLocation is not null) this.OkButton.Enabled = true;
        }

        e.Handled = true;
    }

    private void TagDatabaseService_OnImportError(object? sender, string e)
    {
        Application.Invoke(() =>
        {
            this.importingView.Visible = false;
            this.errorLabel.Visible = true;
            foreach (Button button in this.Buttons) button.Enabled = true;
            MessageBox.ErrorQuery("Error", $"An error occurred trying to import the file:\n{e}", "OK");
        });
    }

    private void TagDatabaseService_OnInitialisationComplete(object? sender, EventArgs e)
    {
        Application.Invoke(() =>
        {
            this.importingView.Visible = false;
            this.errorLabel.Visible = false;
            foreach (Button button in this.Buttons) button.Enabled = true;
            this.RequestStop();
        });
    }
}