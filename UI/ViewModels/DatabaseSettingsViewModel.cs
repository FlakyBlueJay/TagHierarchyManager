using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class DatabaseSettingsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private string _databaseVersion;
    [ObservableProperty] private string _defaultTagBindings;
    [ObservableProperty] private string _tagRowsCount;

    [ObservableProperty] private string _windowTitle;

    public DatabaseSettingsViewModel(MainWindowViewModel mainWindow)
    {
        this._mainWindow = mainWindow;

        this.WindowTitle = mainWindow.TagDatabaseService.DatabaseName;
        this.DatabaseVersion = string.Format(Resources.DatabaseSettingsDbVersion,
            mainWindow.TagDatabaseService.DatabaseVersion);
        var tagRelationshipCount = mainWindow.TagDatabaseService.TagRelationshipCount;
        this.TagRowsCount = string.Format(Resources.DatabaseSettingsTagCount,
            mainWindow.TagDatabaseService.TagCount, tagRelationshipCount);
        this.DefaultTagBindings = string.Join("; ", mainWindow.TagDatabaseService.DefaultTagBindings);
    }


    public void SaveSettings()
    {
        this._mainWindow.TagDatabaseService.SetDefaultTagBindings(this.DefaultTagBindings);
    }
}