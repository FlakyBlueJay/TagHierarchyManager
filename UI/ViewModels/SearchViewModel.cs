using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Common;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class SearchViewModel : ViewModelBase, IDisposable
{
    private readonly Func<List<int>, List<string>> _getParentNamesById;
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private ObservableCollection<TagItemViewModel> _searchResults = [];

    [ObservableProperty] private TagItemViewModel? _selectedSearchResult;

    public SearchViewModel(MainWindowViewModel mainWindow)
    {
        this._mainWindow = mainWindow;
        this._getParentNamesById = parents =>
            parents.Select(id => this._mainWindow.Database?.Tags.FirstOrDefault(t => t.Id == id))
                .Where(tag => tag is not null)
                .Select(tag => tag!.Name)
                .ToList();
        mainWindow.Database?.TagDeleted += this.TagDatabase_OnTagDeleted;
    }

    public void Dispose()
    {
        this._mainWindow.Database?.TagDeleted -= this.TagDatabase_OnTagDeleted;
    }

    public void Search(string searchQuery, TagDatabaseSearchMode mode, bool searchAliases)
    {
        if (this._mainWindow.Database == null || string.IsNullOrWhiteSpace(searchQuery)) return;

        var results = searchAliases
            ? this._mainWindow.Database.SearchWithAliases(searchQuery, mode)
            : this._mainWindow.Database.Search(searchQuery, mode);

        this.SearchResults.Clear();
        if (results.Count == 0)
        {
            this._mainWindow.StatusBlockText = Resources.SearchNoResultsFound;
            return;
        }

        results.Select(tag =>
                new TagItemViewModel(tag, this._getParentNamesById))
            .OrderBy(tag => tag.Name)
            .ToList()
            .ForEach(this.SearchResults.Add);

        this._mainWindow.StatusBlockText = results.Count > 1
            ? string.Format(Resources.SearchMultipleResultsFound, results.Count)
            : Resources.SearchOneResultFound;
    }

    partial void OnSelectedSearchResultChanged(TagItemViewModel? value)
    {
        if (value is null) return;
        this._mainWindow.SelectedTag = value;
    }

    private void TagDatabase_OnTagDeleted(object? sender, (int id, string name) deletedTag)
    {
        var deletedTagItemVm = this.SearchResults.FirstOrDefault(item => item.Tag.Id == deletedTag.id);
        if (deletedTagItemVm is null) return;
        this.SearchResults.Remove(deletedTagItemVm);
    }
}