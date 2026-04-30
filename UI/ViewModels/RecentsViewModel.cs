using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.UI.ViewModels;

public partial class RecentsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private ObservableCollection<RecentsTagRow> _recentAdded = [];
    [ObservableProperty] private ObservableCollection<RecentsTagRow> _recentEdited = [];

    public RecentsViewModel(MainWindowViewModel mainWindow)
    {
        this._mainWindow = mainWindow;
        this.RecentAdded = [];
        foreach (var tag in this._mainWindow.TagDatabaseService.GetRecentTags(true))
            if (tag.CreatedAt is not null)
                this.RecentAdded.Add(new RecentsTagRow(tag, tag.CreatedAt.Value));

        foreach (var tag in this._mainWindow.TagDatabaseService.GetRecentTags(false))
        {
            if (Equals(tag.UpdatedAt, tag.CreatedAt)) continue;
            this.RecentEdited.Add(new RecentsTagRow(tag, tag.UpdatedAt));
        }
    }

    public event Action? RequestClose;

    public void ActivateRowToTagItem(RecentsTagRow tagRow)
    {
        var tag = this._mainWindow.TagDatabaseService.GetTagById(tagRow.Id);
        if (tag is null) return;
        this._mainWindow.SelectedTag = new TagItemViewModel(tag, this._mainWindow.GetParentNamesById);
        this.RequestClose?.Invoke();
    }

    public class RecentsTagRow(Tag tag, DateTime dateTime)
    {
        public DateTime DateTime => dateTime;
        public int Id => tag.Id;
        public string Name => tag.Name;
    }
}