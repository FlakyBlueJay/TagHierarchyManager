using TagHierarchyManager.Models;
using TagHierarchyManager.UI.TerminalUI.Services;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TagHierarchyManager.UI.TerminalUI.Views;

/// <summary>
///     A paned <see cref="View" /> containing the tag editor functionality of the application.
/// </summary>
public class TagEditorPane : View
{
    private readonly TextField aliasesField = new();
    private readonly TextField bindingsField = new();

    private readonly TextField nameField = new();
    private readonly TextView notesField = new();
    private readonly TextField parentsField = new();
    private readonly List<TextField> textFields;
    private readonly CheckBox topLevelCheckBox = new();
    private Tag? currentTag;
    private Button deleteButton = null!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TagEditorPane" /> class.
    /// </summary>
    public TagEditorPane()
    {
        this.InitialiseUI();
        this.textFields = [this.nameField, this.parentsField, this.bindingsField, this.aliasesField];
    }

    internal bool CheckForUnsavedChanges()
    {
        return this.textFields.Any(f => f.HasHistoryChanges) || this.notesField.HasHistoryChanges;
    }

    internal void EnableIfTagLoaded()
    {
        this.Enabled = this.currentTag is not null;
    }

    internal void LoadTag(Tag tag)
    {
        this.currentTag = tag;
        this.UpdateTagDetails();
        this.Enabled = true;
    }

    private void AddButton_OnAccepting(object? sender, CommandEventArgs e)
    {
        if (Program.CurrentDatabase != null)
            this.currentTag = new Tag
            {
                Name = string.Empty,
                IsTopLevel = false,
                TagBindings = Program.CurrentDatabase.DefaultTagBindings,
            };
        else return;

        if (!this.Enabled) this.Enabled = true;
        this.deleteButton.Enabled = false;
        this.UpdateTagDetails();
        e.Handled = true;
    }

    private void ClearTextFieldHistory()
    {
        this.textFields.ForEach(f => f.ClearHistoryChanges());
        this.notesField.ClearHistoryChanges();
    }

    private async void DeleteButton_OnAccepting(object? sender, EventArgs e)
    {
        try
        {
            if (this.currentTag is null) return;
            int result = MessageBox.Query("Are you sure?",
                $"Are you sure you want to delete the tag '{this.currentTag.Name}'?", 1, "Yes", "No");
            if (result != 0) return;

            await TagDatabaseService.DeleteTag(this.currentTag);
            this.currentTag = null;
            this.Disable();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(
                "Error",
                $"An error has occurred trying to delete the tag.\n\nError message:\n{ex.Message}",
                "OK");
        }
    }

    private void Disable()
    {
        this.Enabled = false;
        this.textFields.ForEach(f => f.Text = string.Empty);
        this.notesField.Text = string.Empty;
        this.ClearTextFieldHistory();
        this.topLevelCheckBox.CheckedState = CheckState.UnChecked;
    }

    private void InitialiseUI()
    {
        Label nameLabel = new()
        {
            X = 0,
            Text = "Name:    ",
        };
        Label parentsLabel = new()
        {
            Y = Pos.Bottom(nameLabel),
            Text = "Parents: ",
            CanFocus = false,
        };
        Label bindingsLabel = new()
        {
            Y = Pos.Bottom(parentsLabel),
            Text = "Tag bindings: ",
            CanFocus = false,
        };
        Label aliasesLabel = new()
        {
            Y = Pos.Bottom(bindingsLabel),
            Text = "A.K.As:       ",
            CanFocus = false,
        };

        this.nameField.X = Pos.Right(nameLabel);
        this.nameField.Width = Dim.Fill();
        this.nameField.CanFocus = true;
        this.nameField.Height = 1;

        this.parentsField.X = Pos.Right(parentsLabel);
        this.parentsField.Y = Pos.Bottom(this.nameField);
        this.parentsField.Width = Dim.Fill();
        this.parentsField.CanFocus = true;
        this.parentsField.Height = 1;

        this.bindingsField.Width = Dim.Fill();
        this.bindingsField.X = Pos.Right(bindingsLabel);
        this.bindingsField.Y = Pos.Bottom(this.parentsField);
        this.bindingsField.CanFocus = true;
        this.bindingsField.Height = 1;

        this.aliasesField.Width = Dim.Fill();
        this.aliasesField.X = Pos.Right(aliasesLabel);
        this.aliasesField.Y = Pos.Bottom(this.bindingsField);
        this.aliasesField.CanFocus = true;
        this.aliasesField.Height = 1;

        this.Add(nameLabel, parentsLabel, bindingsLabel, aliasesLabel);
        this.Add(this.nameField, this.parentsField, this.bindingsField, this.aliasesField);

        FrameView notesView = new()
        {
            Height = Dim.Fill() ! - 2,
            Width = Dim.Fill(),
            Y = Pos.Bottom(this.aliasesField),
            Title = "Notes",
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
        };

        this.notesField.Height = Dim.Fill();
        this.notesField.Width = Dim.Fill();
        this.notesField.CanFocus = true;
        this.notesField.TabStop = TabBehavior.TabStop;

        notesView.Add(this.notesField);

        const int buttonWidth = 10;
        const int buttonSpacing = 2;

        this.topLevelCheckBox.Title = "Is top level";
        this.topLevelCheckBox.CanFocus = true;
        this.topLevelCheckBox.X = 0;
        this.topLevelCheckBox.Y = Pos.Bottom(notesView);
        this.topLevelCheckBox.Height = Dim.Fill();
        this.topLevelCheckBox.Width = Dim.Fill();

        Button saveButton = new()
        {
            Title = "Save",
            X = Pos.AnchorEnd(buttonWidth),
            Y = Pos.Bottom(notesView),
            CanFocus = true,
        };
        Button cancelButton = new()
        {
            Title = "Cancel",
            X = Pos.Left(saveButton) - buttonSpacing - buttonWidth,
            Y = Pos.Bottom(notesView),
            CanFocus = true,
        };
        this.deleteButton = new Button
        {
            Title = "Delete",
            X = Pos.Left(cancelButton) - buttonSpacing - buttonWidth,
            Y = Pos.Bottom(notesView),
            CanFocus = true,
        };

        this.Add(notesView);
        this.Add(this.topLevelCheckBox, this.deleteButton, cancelButton, saveButton);
        this.Initialized += this.OnInitialised;
        this.deleteButton.Accepting += this.DeleteButton_OnAccepting;
        saveButton.Accepting += this.SaveButton_OnAccepting;
        cancelButton.Accepting += (_, _) =>
        {
            if (this.currentTag is not null)
                this.UpdateTagDetails(false);
        };
        TagDatabaseService.InitialisationComplete += this.TagDatabaseService_OnInitialised;
    }

    private void OnInitialised(object? sender, EventArgs e)
    {
        Program.TopView.Window.TagPane.NewTagButton.Accepting += this.AddButton_OnAccepting;
    }

    private async void SaveButton_OnAccepting(object? sender, CommandEventArgs e)
    {
        try
        {
            if (this.currentTag is null) return;

            this.currentTag.Name = this.nameField.Text;
            this.currentTag.Notes = this.notesField.Text;
            this.currentTag.IsTopLevel = this.topLevelCheckBox.CheckedState == CheckState.Checked;
            this.currentTag.Parents = this.parentsField.Text.Split(
                ";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            this.currentTag.TagBindings = this.bindingsField.Text.Replace("::", string.Empty)
                .Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            this.currentTag.Aliases = this.aliasesField.Text.Split(
                ";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            this.currentTag.Validate();

            await TagDatabaseService.WriteTagToDatabase(this.currentTag);

            this.ClearTextFieldHistory();
            this.deleteButton.Enabled = true;

            e.Handled = true;
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(
                "Error",
                $"An error has occurred trying to save the tag.\n\nError message:\n{ex.Message}",
                "OK");
            e.Handled = true;
        }
    }

    private void TagDatabaseService_OnInitialised(object? sender, EventArgs e)
    {
        if (sender is not TagDatabase) return;
        this.Disable();
        this.currentTag = null;
    }

    private void UpdateTagDetails(bool clearHistory = true)
    {
        if (this.currentTag is null) return;
        this.nameField.Text = this.currentTag.Name;
        this.notesField.Text = this.currentTag.Notes;
        this.parentsField.Text = string.Join("; ", this.currentTag.Parents);
        this.bindingsField.Text = string.Join("; ", this.currentTag.TagBindings);
        this.aliasesField.Text = string.Join("; ", this.currentTag.Aliases);

        this.topLevelCheckBox.CheckedState = this.currentTag.IsTopLevel ? CheckState.Checked : CheckState.UnChecked;

        if (!clearHistory) return;
        this.textFields.ForEach(f => f.ClearHistoryChanges());
        this.notesField.ClearHistoryChanges();
    }
}