using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.UI.ViewModels;

public partial class RecentsViewModel : ViewModelBase
{
    private readonly Func<List<int>, List<string>> _getParentNamesById;
    private readonly MainWindowViewModel _mainWindow;
    [ObservableProperty] private ObservableCollection<TagRow> _recentAdded = [];
    [ObservableProperty] private ObservableCollection<TagRow> _recentEdited = [];

    public RecentsViewModel(MainWindowViewModel mainWindow)
    {
        this._mainWindow = mainWindow;
        this._getParentNamesById = mainWindow.TagDatabaseService.GetParentNamesByIds;
        this.RecentAdded = new ObservableCollection<TagRow>();
        foreach (var tag in this._mainWindow.TagDatabaseService.GetRecentTags(true))
            if (tag.CreatedAt is not null)
                this.RecentAdded.Add(new TagRow(tag, tag.CreatedAt.Value));

        foreach (var tag in this._mainWindow.TagDatabaseService.GetRecentTags(false))
            this.RecentEdited.Add(new TagRow(tag, tag.UpdatedAt));
    }

    public event Action? RequestClose;

    public void ActivateRowToTagItem(TagRow tagRow)
    {
        var tag = this._mainWindow.TagDatabaseService.GetTagById(tagRow.Id);
        if (tag is null) return;
        var vm = new TagItemViewModel(tag, this._getParentNamesById);
        this._mainWindow.SelectedTag = vm;
        this.RequestClose?.Invoke();
    }

    public class TagRow(Tag tag, DateTime dateTime)
    {
        public DateTime DateTime => dateTime;
        public int Id => tag.Id;
        public string Name => tag.Name;
    }
}