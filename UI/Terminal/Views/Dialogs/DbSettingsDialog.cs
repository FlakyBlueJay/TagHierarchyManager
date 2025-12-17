using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TagHierarchyManager.UI.TerminalUI.Views;

/// <summary>
///     A <see cref="UserInputDialog" /> for handling TagDatabase settings.
/// </summary>
internal class DbSettingsDialog : UserInputDialog
{
    private readonly TextField defaultTagBindingField = new()
    {
        X = 0,
        Width = Dim.Fill(),
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="DbSettingsDialog" /> class.
    /// </summary>
    public DbSettingsDialog()
    {
        this.InitialiseUI();
    }

    private void InitialiseUI()
    {
        this.Title = $"Settings - {Program.CurrentDatabase!.Name}";
        this.Width = 70;
        this.Height = 12;

        Label schemaVersionLabel = new()
        {
            X = 0,
            Y = 0,
            Title = $"Database version: {Program.CurrentDatabase.Version}",
        };

        Label defaultTagBindingLabel = new()
        {
            X = 0,
            Y = Pos.Bottom(schemaVersionLabel) + 1,
            Title = "Default tag binding (separated by semi-colon):",
        };
        this.defaultTagBindingField.Y = Pos.Bottom(defaultTagBindingLabel);
        this.defaultTagBindingField.Text = string.Join(';', Program.CurrentDatabase.DefaultTagBindings);
        this.OkButton.Accepting += this.OkButton_OnAccepting;

        this.Add(schemaVersionLabel, defaultTagBindingLabel, this.defaultTagBindingField);
    }

    private void OkButton_OnAccepting(object? sender, EventArgs e)
    {
        string newDefaultTagBindings = this.defaultTagBindingField.Text.Replace("::", string.Empty);
        Program.CurrentDatabase!.DefaultTagBindings =
            newDefaultTagBindings.Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        Program.Logger.Debug("{@TagBindings}", Program.CurrentDatabase.DefaultTagBindings);
        Application.RequestStop();
    }
}