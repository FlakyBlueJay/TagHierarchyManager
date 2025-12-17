using TagHierarchyManager.Models;
using TagHierarchyManager.UI.TerminalUI.Services;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TagHierarchyManager.UI.TerminalUI.Views;

/// <summary>
///     A <see cref="Window" /> comprising of a <see cref="TagBrowserPane" /> for browsing the tag hierarchy, and a
///     <see cref="TagEditorPane" /> for editing tag data.
/// </summary>
// tbh this could be static, but i think i'm done doing this for now lol.
public class EditorWindow : Window
{
    internal ImportDialog? ImportDialog = null;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EditorWindow" /> class.
    /// </summary>
    /// <param name="topMenu">The top menu to base the window position on.</param>
    public EditorWindow(TopMenu topMenu)
    {
        this.BorderStyle = LineStyle.None;
        this.Y = Pos.Bottom(topMenu);

        this.TagPane = new TagBrowserPane
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(20),
            Height = Dim.Fill()! - 1,
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
        };
        this.EditPane = new TagEditorPane
        {
            X = Pos.Right(this.TagPane),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()! - 1,
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
            Enabled = false,
        };
        this.StatusBar = new MultiModeStatusBar();

        this.Add(this.TagPane, this.EditPane, this.StatusBar);
        TagDatabaseService.InitialisationComplete += this.TagDatabaseService_InitialisationComplete;
        TagDatabaseService.DatabaseInitialising += this.TagDatabaseService_OnDatabaseInitialising;
        TagDatabaseService.ImportError += this.TagDatabaseService_OnImportError;
        TagDatabaseService.InitialisationError += this.TagDatabaseService_OnInitialisationError;
        TagDatabaseService.ExportError += this.TagDatabaseService_OnExportError;
        TagDatabaseService.ExportStarted += this.TagDatabaseService_OnExportStarted;
        TagDatabaseService.ExportCompleted += this.TagDatabaseService_OnExportCompleted;
    }

    /// <summary>
    ///     Gets the <see cref="TagEditorPane" /> inside the window.
    /// </summary>
    internal TagEditorPane EditPane { get; }


    /// <summary>
    ///     Gets the <see cref="TagBrowserPane" /> inside the window.
    /// </summary>
    internal TagBrowserPane TagPane { get; }

    private MultiModeStatusBar StatusBar { get; }

    private void TagDatabaseService_InitialisationComplete(object? sender, EventArgs e)
    {
        if (sender is not TagDatabase) return;
        Application.Invoke(() =>
        {
            this.Enabled = true;
            this.EditPane.Enabled = false;
        });
    }


    private void TagDatabaseService_OnDatabaseInitialising(object? sender, EventArgs e)
    {
        Application.Invoke(() => { this.Enabled = false; });
    }

    private void TagDatabaseService_OnExportCompleted(object? sender, string e)
    {
        Application.Invoke(() =>
        {
            this.Enabled = true;
            this.EditPane.EnableIfTagLoaded();
        });
    }

    private void TagDatabaseService_OnExportError(object? sender, string e)
    {
        Application.Invoke(() =>
        {
            MessageBox.ErrorQuery("Error",
                $"An error occurred during the export process.\n\nError message:\n{e}",
                "OK");
            this.Enabled = true;
            this.EditPane.EnableIfTagLoaded();
        });
    }

    private void TagDatabaseService_OnExportStarted(object? sender, EventArgs e)
    {
        Application.Invoke(() => { this.Enabled = false; });
    }


    private void TagDatabaseService_OnImportError(object? sender, string e)
    {
        Application.Invoke(() =>
        {
            this.Enabled = true;
            this.EditPane.Enabled = false;
        });
    }

    private void TagDatabaseService_OnInitialisationError(object? sender, string errorMessage)
    {
        Application.Invoke(() =>
        {
            MessageBox.ErrorQuery("Error",
                $"An error has occurred trying to initialise the database.\n\nError message:\n{errorMessage}",
                "OK");
            if (Program.CurrentDatabase == null) return;
            this.Enabled = true;
            this.EditPane.EnableIfTagLoaded();
        });
    }
}