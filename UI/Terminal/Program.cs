using Serilog;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.TerminalUI.Views;
using Terminal.Gui.App;

namespace TagHierarchyManager.UI.TerminalUI;

/// <summary>
///     The class that the application will execute at first.
/// </summary>
internal static class Program
{
    /// <summary>
    ///     A logger for debugging purposes that logs to any compatible .NET debugger, using Serilog.
    /// </summary>
    public static readonly ILogger Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Debug()
        .CreateLogger();

    /// <summary>
    ///     Gets the top-level view, which also encapsulates the main window of the application.
    /// </summary>
    internal static readonly TopView TopView = new();

    /// <summary>
    ///     Gets the current database associated with the running process of the application.
    /// </summary>
    internal static TagDatabase? CurrentDatabase = null!;

    /// <summary>
    ///     Gets the main editor window of the application, essentially acting as a shortcut.
    /// </summary>
    internal static EditorWindow MainView => TopView.Window;

    /// <summary>
    ///     The method the .NET runtime will use to execute the application.
    /// </summary>
    public static void Main()
    {
        try
        {
            Application.Init();
            Application.Run(TopView);
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "FATAL ERROR WAS CAUGHT.");
        }
        finally
        {
            Application.Shutdown();
        }
    }
}