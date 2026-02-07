using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class BulkAddViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindow;
    [ObservableProperty] private ObservableCollection<TagRow> _tags = [new() { Name = "", IsTopLevel = false }];

    [ObservableProperty] private string _windowTitle;

    public BulkAddViewModel(MainWindowViewModel mainWindow)
    {
        this._mainWindow = mainWindow;
        this.Tags.CollectionChanged += this.OnTagsCollectionChanged;
        this.WindowTitle = string.Format("{0} - " + Resources.ButtonBulkAdd,
            this._mainWindow.TagDatabaseService.DatabaseName);
    }

    public bool CanSave => this.Tags.Count > 0;

    public void AddTag()
    {
        this.Tags.Add(new TagRow());
    }

    public void RemoveTags(HashSet<TagRow> tags)
    {
        foreach (var tag in tags) this.Tags.Remove(tag);
    }

    public async Task SaveTags()
    {
        if (!this._mainWindow.TagDatabaseService.IsDatabaseOpen) return;
        var tags = this.Tags.Select(tagRow =>
            {
                var tag = new Tag
                {
                    Name = tagRow.Name,
                    IsTopLevel = tagRow.IsTopLevel,
                    Parents = !string.IsNullOrWhiteSpace(tagRow.Parents)
                        ? tagRow.Parents.Split(';',
                                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToList()
                        : [],
                    TagBindings = !string.IsNullOrWhiteSpace(tagRow.TagBindings)
                        ? tagRow.TagBindings.Split(';',
                                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToList()
                        : [],
                    Aliases = !string.IsNullOrWhiteSpace(tagRow.Aliases)
                        ? tagRow.Aliases.Split(';',
                                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToList()
                        : [],
                    Notes = !string.IsNullOrEmpty(tagRow.Notes) ? tagRow.Notes : string.Empty
                };
                tag.Validate();
                return tag;
            })
            .ToList();

        await this._mainWindow.TagDatabaseService.WriteTagsToDatabase(tags);
        this._mainWindow.StatusBlockText = string.Format(Resources.StatusBlockBulkAddSuccess, tags.Count);
    }

    private void OnTagsCollectionChanged(object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        this.OnPropertyChanged(nameof(this.CanSave));
    }

    public class TagRow
    {
        public string Aliases { get; set; } = string.Empty;
        public bool IsTopLevel { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public string Parents { get; set; } = string.Empty;
        public string TagBindings { get; set; } = string.Empty;
    }
}