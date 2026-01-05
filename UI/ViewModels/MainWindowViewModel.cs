using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.UI.ViewModels;

// TODO TagItemViewModel

public partial class MainWindowViewModel : ViewModelBase
{
    private TagDatabase _database;
    
    [ObservableProperty]
    private TagItemViewModel? _selectedTag;
    
    [ObservableProperty]
    private ObservableCollection<TagItemViewModel> _topLevelTags = [];

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
                this.TopLevelTags.Clear();
                var tagMap = db.Tags.ToDictionary(t => t.Id, t => new TagItemViewModel(t));
                
                foreach (var tag in db.Tags.OrderBy(t => t.Name))
                {
                    var viewModel = tagMap[tag.Id];
                    foreach (var parentId in tag.ParentIds)
                    {
                        if (tagMap.TryGetValue(parentId, out var parentViewModel))
                        {
                            parentViewModel.Children.Add(viewModel);
                        }
                    }
                }
                foreach (var tag in db.Tags.Where(t => t.IsTopLevel))
                {
                    this.TopLevelTags.Add(tagMap[tag.Id]);
                }
            });
            Debug.WriteLine($"Database loaded on UI - name: {db.Name}, version: {db.Version}");
        };
        await db.LoadAsync(filePath);
    }
}