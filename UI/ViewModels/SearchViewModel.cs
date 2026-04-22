using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagHierarchyManager.Common;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class SearchViewModel : ViewModelBase, IDisposable
{
    private readonly MainWindowViewModel _mainWindow;
    private readonly Func<List<int>, List<string>> _getParentNamesById;

    [ObservableProperty] private ObservableCollection<TagItemViewModel> _searchResults = [];

    [ObservableProperty] private TagItemViewModel? _selectedSearchResult;

    public SearchViewModel(MainWindowViewModel mainWindow)
    {
        this._mainWindow = mainWindow;
        this._getParentNamesById = this._mainWindow.GetParentNamesById;
        mainWindow.TagDatabaseService.TagsWritten += this.TagDatabase_OnTagsWritten;
    }

    public void Dispose()
    {
        this._mainWindow.TagDatabaseService.TagsWritten -= this.TagDatabase_OnTagsWritten;
    }

    partial void OnSelectedSearchResultChanged(TagItemViewModel? value)
    {
        if (value is null || this._mainWindow.SelectedTag?.Id == value.Id) return;
        this._mainWindow.SelectedTag = value;
    }

    private void Search(string searchQuery, TagDatabaseSearchMode mode, bool searchAliases)
    {
        if (!this._mainWindow.TagDatabaseService.IsDatabaseOpen || string.IsNullOrWhiteSpace(searchQuery)) return;

        this.SearchResults.Clear();

        var results = this._mainWindow.TagDatabaseService.SearchTags(searchQuery, mode, searchAliases);

        if (results.Count == 0)
        {
            this._mainWindow.StatusBlockText = Resources.SearchNoResultsFound;
            return;
        }

        results.Select(tag =>
                new TagItemViewModel(tag, this._getParentNamesById))
            .OrderBy(tag => tag.CurrentName)
            .ToList()
            .ForEach(this.SearchResults.Add);

        this._mainWindow.StatusBlockText = results.Count > 1
            ? string.Format(Resources.SearchMultipleResultsFound, results.Count)
            : Resources.SearchOneResultFound;
    }


    [RelayCommand]
    private void StartSearch(object? parameter)
    {
        if (parameter is not object[] values || values.Length < 3) return;

        var query = values[0] as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query)) return;
        var mode = (TagDatabaseSearchMode)values[1];
        var searchAliases = (bool)values[2];

        try
        {
            this.Search(query, mode, searchAliases);
        }
        catch (Exception ex)
        {
            this._mainWindow.ShowErrorDialog(ex.Message);
        }
    }

    private void TagDatabase_OnTagsWritten(object? sender, TagDatabaseService.TagWriteResult result)
    {
        if (sender is not TagDatabaseService { IsDatabaseOpen: true }) return;
        if (result.Deleted.Count == 0) return;

        foreach (var deletedTag in result.Deleted)
        {
            var deletedTagItemVm = this.SearchResults.FirstOrDefault(item => item.Tag.Id == deletedTag.id);
            if (deletedTagItemVm is null) return;
            this.SearchResults.Remove(deletedTagItemVm);
        }
    }
}