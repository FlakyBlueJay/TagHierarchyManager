using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class BulkAddViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private ObservableCollection<TagRow> _selectedRows = [];

    [ObservableProperty] private ObservableCollection<TagRow> _tags = [new() { Name = "", IsTopLevel = false }];

    [ObservableProperty] private string _windowTitle;

    public BulkAddViewModel(MainWindowViewModel mainWindow)
    {
        this._mainWindow = mainWindow;

        this.WindowTitle = string.Format("{0} - " + Resources.ButtonBulkAdd,
            this._mainWindow.TagDatabaseService.DatabaseName);

        this.OnSelectedRowsChanged(null, this.SelectedRows);
    }

    public event Action? RequestClose;

    public bool CanDeleteRows => this.Tags.Count - this.SelectedRows.Count > 0 && this.Tags.Count > 1;

    public bool CanSave => this.Tags.Count > 0;

    [RelayCommand]
    private void AddTag()
    {
        this.Tags.Add(new TagRow());
    }

    partial void OnSelectedRowsChanged(ObservableCollection<TagRow>? oldValue, ObservableCollection<TagRow> newValue)
    {
        oldValue?.CollectionChanged -= this.OnSelectedRowsCollectionChanged;
        newValue.CollectionChanged += this.OnSelectedRowsCollectionChanged;
        this.OnPropertyChanged(nameof(this.CanDeleteRows));
    }

    private void OnSelectedRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.OnPropertyChanged(nameof(this.CanDeleteRows));
    }

    [RelayCommand]
    private void RemoveTags()
    {
        if (!this.CanDeleteRows) return;
        foreach (var tag in this.SelectedRows.ToList()) this.Tags.Remove(tag);
    }

    [RelayCommand]
    private async Task SaveTags()
    {
        if (!this.CanSave) return;
        var currentTagRow = 0;
        try
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
                    currentTagRow++;
                    return tag;
                })
                .ToList();

            await this._mainWindow.TagDatabaseService.WriteTagsToDatabase(tags);
            this._mainWindow.StatusBlockText = string.Format(Resources.StatusBlockBulkAddSuccess, tags.Count);
            this.RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var error = new ErrorDialogViewModel(
                    string.Format(Resources.BulkAddExceptionTemplate, ex.Message, currentTagRow + 1));
                error.ShowDialog();
            });
        }
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