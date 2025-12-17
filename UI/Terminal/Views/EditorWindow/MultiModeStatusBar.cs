using TagHierarchyManager.Models;
using TagHierarchyManager.UI.TerminalUI.Services;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TagHierarchyManager.UI.TerminalUI.Views;

public class MultiModeStatusBar : View
{
    private TextOnly textOnlyView = null!;
    private TextWithSpinner textWithSpinnerView = null!;

    public MultiModeStatusBar()
    {
        this.InitialiseUI();
    }

    private void InitialiseUI()
    {
        this.X = 0;
        this.Y = Pos.AnchorEnd(1);
        this.Width = Dim.Fill();
        this.Height = 1;
        this.Visible = true;
        this.CanFocus = false;

        this.textWithSpinnerView = new TextWithSpinner
        {
            Visible = false,
        };

        this.textOnlyView = new TextOnly
        {
            Visible = true,
        };
        // Set the label text directly
        this.textOnlyView.TextLabel.Text = "Ready.";

        this.Add(this.textWithSpinnerView, this.textOnlyView);
        TagDatabaseService.InitialisationError += this.TagDatabaseService_OnInitialisationError;
        TagDatabaseService.InitialisationComplete += this.TagDatabaseService_InitialisationComplete;
        TagDatabaseService.ImportError += this.TagDatabaseService_OnImportError;
        TagDatabaseService.DatabaseInitialising += this.TagDatabaseService_OnDatabaseInitialising;
        TagDatabaseService.TagSaved += this.TagDatabaseService_OnTagSaved;
        TagDatabaseService.TagDeleted += this.TagDatabaseService_OnTagDeleted;
        TagDatabaseService.ExportStarted += this.TagDatabaseService_OnExportStarted;
        TagDatabaseService.ExportCompleted += this.TagDatabaseService_OnExportCompleted;
        TagDatabaseService.ExportError += this.TagDatabaseService_OnExportError;
        TagDatabaseService.SearchFinished += this.TagDatabaseService_OnSearchFinished;
    }

    private void TagDatabaseService_InitialisationComplete(object? sender, EventArgs e)
    {
        this.UpdateStatusBarTextOnly("Database initialized successfully.");
    }

    private void TagDatabaseService_OnDatabaseInitialising(object? sender, EventArgs e)
    {
        this.UpdateStatusBarWithSpinner("Initializing database...");
    }

    private void TagDatabaseService_OnExportCompleted(object? sender, string filePath)
    {
        this.UpdateStatusBarTextOnly($"Database exported successfully to \"{filePath}\".");
    }

    private void TagDatabaseService_OnExportError(object? sender, string e)
    {
        this.UpdateStatusBarTextOnly("Export failed.");
    }

    private void TagDatabaseService_OnExportStarted(object? sender, EventArgs e)
    {
        this.UpdateStatusBarWithSpinner("Exporting database...");
    }

    private void TagDatabaseService_OnImportError(object? sender, string e)
    {
        this.UpdateStatusBarTextOnly("Error importing: " + e);
    }
    
    private void TagDatabaseService_OnSearchFinished(object? sender, List<Tag> result)
    {
        if (result.Count == 0) this.Clear();
        else this.UpdateStatusBarTextOnly(result.Count == 1 ? "One result found." : $"{result.Count} results found.");
    }

    private void TagDatabaseService_OnInitialisationError(object? sender, string e)
    {
        this.UpdateStatusBarTextOnly("Error initializing database: " + e);
    }

    private void TagDatabaseService_OnTagDeleted(object? sender, (int tagId, string tagName) tagData)
    {
        this.UpdateStatusBarTextOnly($"Tag \"{tagData.tagName}\" deleted successfully.");
    }

    private void TagDatabaseService_OnTagSaved(object? sender, Tag tag)
    {
        this.UpdateStatusBarTextOnly($"Tag \"{tag.Name}\" saved successfully.");
    }

    private void Clear()
    {
        Application.Invoke(() =>
        {
            this.textWithSpinnerView.Visible = false;
            this.textOnlyView.Visible = false;
        });
    }
    
    private void UpdateStatusBarTextOnly(string message)
    {
        Application.Invoke(() =>
        {
            this.textOnlyView.TextLabel.Text = message;
            this.textWithSpinnerView.Visible = false;
            this.textOnlyView.Visible = true;
        });
    }

    private void UpdateStatusBarWithSpinner(string message)
    {
        Application.Invoke(() =>
        {
            this.textWithSpinnerView.TextLabel.Text = message;
            this.textWithSpinnerView.Visible = true;
            this.textOnlyView.Visible = false;
        });
    }

    private sealed class TextOnly : View
    {
        internal readonly Label TextLabel;

        public TextOnly()
        {
            this.X = 0;
            this.Y = 0;
            this.Width = Dim.Fill();
            this.Height = 1;

            this.TextLabel = new Label
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 1,
                Text = string.Empty,
            };

            this.Add(this.TextLabel);
        }
    }

    private sealed class TextWithSpinner : View
    {
        internal readonly Label TextLabel;

        public TextWithSpinner()
        {
            this.X = 0;
            this.Y = 0;
            this.Width = Dim.Fill();
            this.Height = 1;

            SpinnerView spinner = new()
            {
                X = 0,
                Y = 0,
                Width = 1,
                Height = 1,
                AutoSpin = true,
                Style = new SpinnerStyle.Dots(),
            };

            this.TextLabel = new Label
            {
                X = Pos.Right(spinner) + 1,
                Y = 0,
                Width = Dim.Fill(),
                Height = 1,
                Text = string.Empty,
            };

            this.Add(spinner, this.TextLabel);
        }
    }
}