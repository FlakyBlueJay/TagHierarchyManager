using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TagHierarchyManager.UI.ViewModels;

public partial class DatabaseSettingsViewModel : ViewModelBase
{
    public DatabaseSettingsViewModel(MainWindowViewModel mainWindow)
    {
        this.MainWindow = mainWindow;
        
        this.WindowTitle = mainWindow.Database!.Name;
        this.DatabaseVersion = string.Format(Assets.Resources.DatabaseSettingsDbVersion, mainWindow.Database!.Version);
        var tagRelationshipCount = mainWindow.Database!.GetTagRelationshipCount();
        this.TagRowsCount = string.Format(Assets.Resources.DatabaseSettingsTagCount,
            mainWindow.Database!.Tags.Count, tagRelationshipCount);
        this.DefaultTagBindings = string.Join("; ", mainWindow.Database!.DefaultTagBindings);
    }
    
    private MainWindowViewModel MainWindow { get; }

    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private string _databaseVersion;
    [ObservableProperty] private string _tagRowsCount;
    [ObservableProperty] private string _defaultTagBindings;
    
    public void SaveSettings()
    {
        this.MainWindow.Database!.DefaultTagBindings = this.DefaultTagBindings.Split(";", 
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}