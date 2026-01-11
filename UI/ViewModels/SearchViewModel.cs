using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Common;

namespace TagHierarchyManager.UI.ViewModels;

public partial class SearchViewModel(MainWindowViewModel mainWindow) : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<TagItemViewModel> _searchResults = [];
    
    [ObservableProperty] private TagItemViewModel? _selectedSearchResult;

    public void Search(string searchQuery, TagDatabaseSearchMode mode, bool searchAliases)
    {
        var results = searchAliases
            ? mainWindow.Database.SearchWithAliases(searchQuery, mode)
            : mainWindow.Database.Search(searchQuery, mode);

        this.SearchResults.Clear();
        if (results.Count == 0)
        {
            // todo resx
            mainWindow.StatusBlockText = "No results found.";
            return;
        }
        
        
        results.Select(tag => new TagItemViewModel(tag, id => 
                mainWindow.Database.Tags.FirstOrDefault(t => t.Id == id)?.Name))
            .OrderBy(tag => tag.Name)
            .ToList()
            .ForEach(this.SearchResults.Add);
        
    }

    partial void OnSelectedSearchResultChanged(TagItemViewModel? value)
    {
        if (value is null) return;
        mainWindow.SelectedTag = value;
    }
}