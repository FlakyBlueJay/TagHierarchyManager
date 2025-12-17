using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TagHierarchyManager.UI.TerminalUI.Views;

/// <summary>
///     A top-level view that encapsulates the main view of the application.
/// </summary>
public class TopView : Toplevel
{
    /// <summary>
    ///     Gets the <see cref="EditorWindow" /> associated with the main view.
    /// </summary>
    public EditorWindow Window = null!;

    private TopMenu topMenu = null!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TopView" /> class.
    /// </summary>
    public TopView()
    {
        this.InitialiseUI();
    }

    private void InitialiseUI()
    {
        // remember to look at the QuitKey property.
        this.topMenu = new TopMenu
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
        };

        this.Window = new EditorWindow(this.topMenu);

        this.Add(this.topMenu, this.Window);
        this.Closing += this.OnToplevelClosing;
    }

    private void OnToplevelClosing(object? sender, ToplevelClosingEventArgs e)
    {
        if (this.Window.EditPane.CheckForUnsavedChanges()) StandardDialogs.CallUnsavedChangesMessageBox(e);
    }
}