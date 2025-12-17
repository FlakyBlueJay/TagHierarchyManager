using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace TagHierarchyManager.UI.TerminalUI.Views;

/// <summary>
///     A base class implementing a <see cref="Dialog" /> template centered around user-facing input.
/// </summary>
public class UserInputDialog : Dialog
{
    protected Button OkButton = null!;
    private Button cancelButton = null!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserInputDialog" /> class.
    /// </summary>
    protected UserInputDialog()
    {
        this.InitialiseUI();
    }

    private void InitialiseUI()
    {
        this.OkButton = new Button
        {
            Title = "OK",
        };

        this.cancelButton = new Button
        {
            Title = "Cancel",
        };

        this.cancelButton.Accepting += (_, _) => Application.RequestStop();

        this.AddButton(this.OkButton);
        this.AddButton(this.cancelButton);
    }
}