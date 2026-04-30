using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagHierarchyManager.Common;
using TagHierarchyManager.UI.Assets;
using TagHierarchyManager.UI.Services;

namespace TagHierarchyManager.UI.ViewModels;

public partial class SearchViewModel : ViewModelBase, IDisposable
{
    private readonly Func<List<int>, List<string>> _getParentNamesById;
    private readonly MainWindowViewModel _mainWindow;
    private readonly DialogService _dialogService;
    [ObservableProperty] private bool _searchAliases;
    [ObservableProperty] private TagDatabaseSearchMode _searchMode = TagDatabaseSearchMode.Fuzzy;

    [ObservableProperty] private string _searchQuery = string.Empty;

    [ObservableProperty] private ObservableCollection<TagItemViewModel> _searchResults = [];
    [ObservableProperty] private SearchModeOption _selectedSearchMode;

    [ObservableProperty] private TagItemViewModel? _selectedSearchResult;

    public SearchViewModel(MainWindowViewModel mainWindow, DialogService dialogService)
    {
        this._mainWindow = mainWindow;
        this._dialogService = dialogService;
        this._getParentNamesById = this._mainWindow.GetParentNamesById;
        mainWindow.TagDatabaseService.TagsWritten += this.TagDatabase_OnTagsWritten;
        this._selectedSearchMode = this.SearchModes.First();
    }

    public IReadOnlyList<SearchModeOption> SearchModes { get; } =
    [
        new(TagDatabaseSearchMode.Fuzzy, Resources.SearchModeFuzzy),
        new(TagDatabaseSearchMode.StartsWith, Resources.SearchModeStartsWith),
        new(TagDatabaseSearchMode.EndsWith, Resources.SearchModeEndsWith),
        new(TagDatabaseSearchMode.ExactMatch, Resources.SearchModeExactMatch)
    ];

    public void Dispose()
    {
        this._mainWindow.TagDatabaseService.TagsWritten -= this.TagDatabase_OnTagsWritten;
    }

    partial void OnSelectedSearchResultChanged(TagItemViewModel? value)
    {
        if (value is null || this._mainWindow.SelectedTag?.Id == value.Id) return;
        this._mainWindow.SelectedTag = value;
    }

    [RelayCommand]
    private void Search()
    {
        if (!this._mainWindow.TagDatabaseService.IsDatabaseOpen || string.IsNullOrWhiteSpace(this.SearchQuery)) return;
        try
        {
            this.SearchResults.Clear();

            var results = this._mainWindow.TagDatabaseService.SearchTags(
                this.SearchQuery, this.SelectedSearchMode.Mode, this.SearchAliases);

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
        catch (Exception ex)
        {
            this._dialogService.ShowErrorDialog(ex.Message);
        }
    }

    private void TagDatabase_OnTagsWritten(object? sender, TagDatabaseService.TagWriteResult result)
    {
        if (sender is not TagDatabaseService { IsDatabaseOpen: true }) return;
        if (result.Deleted.Count == 0) return;

        foreach (var deletedTag in result.Deleted)
        {
            var deletedTagItemVm = this.SearchResults.FirstOrDefault(item => item.Tag.Id == deletedTag.id);
            if (deletedTagItemVm is null) continue;
            this.SearchResults.Remove(deletedTagItemVm);
        }
    }

    public sealed record SearchModeOption(TagDatabaseSearchMode Mode, string DisplayName);
}