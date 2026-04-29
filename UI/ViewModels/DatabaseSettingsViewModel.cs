using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class DatabaseSettingsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private string _databaseVersion;
    [ObservableProperty] private string _defaultTagBindings;
    [ObservableProperty] private string _tagRowsCount;
    [ObservableProperty] private int _tagRelationshipsCount;

    [ObservableProperty] private string _windowTitle;

    public DatabaseSettingsViewModel(MainWindowViewModel mainWindow)
    {
        this._mainWindow = mainWindow;

        this.WindowTitle = mainWindow.TagDatabaseService.DatabaseName;
        this.DatabaseVersion = string.Format(Resources.DatabaseSettingsDbVersion,
            mainWindow.TagDatabaseService.DatabaseVersion);
        this.TagRelationshipsCount = 0;
        this.TagRowsCount = string.Empty;
        this.DefaultTagBindings = string.Join("; ", mainWindow.TagDatabaseService.DefaultTagBindings);
        _ = this.InitialiseAsync(mainWindow);
    }

    private async Task InitialiseAsync(MainWindowViewModel mainWindow)
    {
        var tagRelationshipCount = await mainWindow.TagDatabaseService.GetTagRelationshipCountAsync();
        this.TagRowsCount = string.Format(Resources.DatabaseSettingsTagCount,
            mainWindow.TagDatabaseService.TagCount, tagRelationshipCount);
    }

    public event Action? RequestClose;

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await this._mainWindow.TagDatabaseService.SetDefaultTagBindingsAsync(this.DefaultTagBindings);
        this.RequestClose?.Invoke();
    }
}