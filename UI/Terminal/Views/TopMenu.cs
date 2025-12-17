using TagHierarchyManager.Models;
using TagHierarchyManager.UI.TerminalUI.Services;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TagHierarchyManager.UI.TerminalUI.Views;

/// <summary>
///     The top menu bar of the application.
/// </summary>
public class TopMenu : MenuBarv2
{
    private const string NullDatabaseName = "";
    private const string TotalTagsString = "Total tags: {0}";

    /// <summary>
    ///     The menu item that will initiate the MusicBee tag hierarchy exporting process.
    /// </summary>
    private MenuItemv2 exportToTagHierarchyItem = null!;

    private MenuItemv2 totalTagsItem = null!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TopMenu" /> class.
    /// </summary>
    public TopMenu()
    {
        this.InitialiseUI();
    }

    private MenuBarItemv2 DbMenu { get; set; } = null!;

    private static void OnNewDatabase()
    {
        (string fileName, bool userAllowedOverwrite)? newDatabase = StandardDialogs.NewDatabaseSaveDialog();
        if (newDatabase is null) return;

        TagDatabaseService.CreateDatabase(newDatabase.Value);
    }

    private void InitialiseUI()
    {
        this.exportToTagHierarchyItem = new MenuItemv2(
            "_Export to MusicBee tag hierarchy template",
            string.Empty,
            TagDatabaseService.StartExportProcess)
        {
            Enabled = false,
        };

        this.DbMenu = new MenuBarItemv2(
            NullDatabaseName)
        {
            Enabled = false,
        };

        this.totalTagsItem = new MenuItemv2(
            TotalTagsString,
            string.Empty,
            null);
        this.totalTagsItem.Data = 0;

        MenuBarItemv2 fileMenu = new("_File");
        PopoverMenu filePopover = new([
            new MenuItemv2("_New database...", string.Empty, OnNewDatabase),
            new MenuItemv2(
                "New from MusicBee _tag hierarchy template",
                string.Empty,
                () =>
                {
                    Program.MainView.ImportDialog = new ImportDialog();
                    Application.Run(Program.MainView.ImportDialog);
                }),
            new Line(),
            new MenuItemv2("_Open database...", string.Empty, TagDatabaseService.PromptDatabaseLoad),
            new Line(),
            this.exportToTagHierarchyItem,
            new Line(),
            new MenuItemv2("_Quit", string.Empty, () => Application.RequestStop()),
        ]);

        fileMenu.PopoverMenu = filePopover;

        MenuBarItemv2 helpMenu = new("_Help");
        PopoverMenu helpPopover = new([
            new MenuItemv2("About", string.Empty, StandardDialogs.AboutMessageBox),
        ]);

        helpMenu.PopoverMenu = helpPopover;

        PopoverMenu dbPopover = new([
            new MenuItemv2(
                "Database settings",
                string.Empty,
                () => Application.Run(new DbSettingsDialog())),

            new Line(),
            this.totalTagsItem,
        ]);

        View lineContainer = new()
        {
            Width = 1,
            Height = Dim.Fill(),
        };
        lineContainer.Add(new Line { Orientation = Orientation.Vertical, Width = 1, Height = Dim.Fill() });
        this.DbMenu.PopoverMenu = dbPopover;
        this.Add(fileMenu, helpMenu, lineContainer, this.DbMenu);
        TagDatabaseService.InitialisationComplete += this.TagDatabaseService_OnInitialised;
    }

    private void TagDatabase_OnTagAdded(object? sender, Tag? tag)
    {
        int currentTagCount = (int)this.totalTagsItem.Data!;
        currentTagCount++;
        this.UpdateTotalTagsCount(currentTagCount);
    }

    private void TagDatabase_OnTagDeleted(object? sender, (int, string) tag)
    {
        int currentTagCount = (int)this.totalTagsItem.Data!;
        currentTagCount--;
        this.UpdateTotalTagsCount(currentTagCount);
    }

    private void TagDatabaseService_OnInitialised(object? sender, EventArgs e)
    {
        if (sender is not TagDatabase db) return;
        this.DbMenu.Title = $"Current database: {db.Name}";
        this.DbMenu.Enabled = true;
        this.totalTagsItem.Title = string.Format(TotalTagsString, db.Tags.Count);
        this.totalTagsItem.Data = db.Tags.Count;
        TagDatabaseService.TagAdded += this.TagDatabase_OnTagAdded;
        TagDatabaseService.TagDeleted += this.TagDatabase_OnTagDeleted;
        this.exportToTagHierarchyItem.Enabled = true;
    }

    private void UpdateTotalTagsCount(int newCount)
    {
        this.totalTagsItem.Data = newCount;
        this.totalTagsItem.Title = string.Format(TotalTagsString, newCount);
    }
}