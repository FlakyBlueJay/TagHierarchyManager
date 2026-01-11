using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Common;

namespace TagHierarchyManager.UI.ViewModels;

public partial class SearchViewModel : ViewModelBase, IDisposable
{
    private readonly MainWindowViewModel mainWindow;
    
    public SearchViewModel(MainWindowViewModel mainWindow)
    {
        this.mainWindow = mainWindow;
        mainWindow.Database.TagDeleted += TagDatabase_OnTagDeleted;
    }
    
    [ObservableProperty] private ObservableCollection<TagItemViewModel> _searchResults = [];
    
    [ObservableProperty] private TagItemViewModel? _selectedSearchResult;

    public void Dispose() =>
        mainWindow.Database.TagDeleted -= TagDatabase_OnTagDeleted;

    public void Search(string searchQuery, TagDatabaseSearchMode mode, bool searchAliases)
    {
        if (string.IsNullOrWhiteSpace(searchQuery)) return;
        
        var results = searchAliases
            ? mainWindow.Database.SearchWithAliases(searchQuery, mode)
            : mainWindow.Database.Search(searchQuery, mode);

        string statusBlockString = string.Empty;
        this.SearchResults.Clear();
        if (results.Count == 0)
        {
            // todo resx
            mainWindow.StatusBlockText = Assets.Resources.SearchNoResultsFound;
            return;
        }
        
        results.Select(tag => new TagItemViewModel(tag, id => 
                mainWindow.Database.Tags.FirstOrDefault(t => t.Id == id)?.Name))
            .OrderBy(tag => tag.Name)
            .ToList()
            .ForEach(this.SearchResults.Add);

        mainWindow.StatusBlockText = results.Count > 1
            ? string.Format(Assets.Resources.SearchMultipleResultsFound, results.Count)
            : Assets.Resources.SearchOneResultFound;

    }

    partial void OnSelectedSearchResultChanged(TagItemViewModel? value)
    {
        if (value is null) return;
        mainWindow.SelectedTag = value;
    }
    
    private void TagDatabase_OnTagDeleted(object? sender, (int id, string name) deletedTag)
    {
        var deletedTagItemVM = this.SearchResults.First(item => item.Tag.Id == deletedTag.id);
        this.SearchResults.Remove(deletedTagItemVM);
    }
}