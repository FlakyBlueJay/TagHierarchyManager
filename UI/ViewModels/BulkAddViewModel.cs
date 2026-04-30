using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;
using TagHierarchyManager.UI.Services;

namespace TagHierarchyManager.UI.ViewModels;

public partial class BulkAddViewModel : ViewModelBase
{
    private readonly DialogService _dialogService;
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private ObservableCollection<BulkAddTagRow> _selectedRows = [];

    [ObservableProperty] private ObservableCollection<BulkAddTagRow> _tags;

    [ObservableProperty] private string _windowTitle;

    public BulkAddViewModel(MainWindowViewModel mainWindow, DialogService dialogService)
    {
        this._mainWindow = mainWindow;
        this._dialogService = dialogService;

        this.WindowTitle = $"{this.TagDatabaseService.DatabaseName} - {Resources.ButtonBulkAdd}";

        this._tags =
        [
            new BulkAddTagRow
            {
                Name = "",
                IsTopLevel = false,
                TagBindings = string.Join("; ", this.TagDatabaseService.DefaultTagBindings)
            }
        ];

        this.OnSelectedRowsChanged(null, this.SelectedRows);
    }

    public event Action? RequestClose;

    public bool CanDeleteRows => this.Tags.Count - this.SelectedRows.Count > 0 && this.Tags.Count > 1;

    public bool CanSave => this.Tags.Count > 0;

    private TagDatabaseService TagDatabaseService => this._mainWindow.TagDatabaseService;

    [RelayCommand]
    private void AddTag()
    {
        this.Tags.Add(new BulkAddTagRow
        {
            Name = "",
            IsTopLevel = false,
            TagBindings = string.Join("; ", this._mainWindow.TagDatabaseService.DefaultTagBindings)
        });
    }

    partial void OnSelectedRowsChanged(ObservableCollection<BulkAddTagRow>? oldValue,
        ObservableCollection<BulkAddTagRow> newValue)
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
                    currentTagRow++;
                    return tag;
                })
                .ToList();

            await this._mainWindow.TagDatabaseService.WriteTagsToDatabase(
                tags.Select(t =>
                {
                    var tVm = new TagItemViewModel(t, this.TagDatabaseService.GetParentNamesByIds);
                    tVm.BeginEdit();
                    return tVm;
                }).ToList());
            this._mainWindow.StatusBlockText = string.Format(Resources.StatusBlockBulkAddSuccess, tags.Count);
            this.RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            await this._dialogService.ShowErrorDialog(ex.Message);
        }
    }

    public class BulkAddTagRow
    {
        public string Aliases { get; set; } = string.Empty;
        public bool IsTopLevel { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public string Parents { get; set; } = string.Empty;
        public string TagBindings { get; set; } = string.Empty;
    }
}