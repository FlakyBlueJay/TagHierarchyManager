using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private TagDatabase _database;
    
    public ObservableCollection<Tag> Tags;

    [ObservableProperty]
    private bool _isDbLoaded;

    // TODO searchViewModel
    // TODO hierarchyViewModel
    
    public MainWindowViewModel()
    {

    }

    public async Task LoadDatabase(string filePath)
    {
        TagDatabase db = new();
        db.InitialisationComplete += (s, e) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                this._database = db;
                this.IsDbLoaded = true;
            });
            Debug.WriteLine($"Database loaded on UI - name: {db.Name}, version: {db.Version}");
        };
        await db.LoadAsync(filePath);
    }
}