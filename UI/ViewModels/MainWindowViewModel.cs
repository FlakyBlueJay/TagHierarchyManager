using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Common;
using TagHierarchyManager.Exporters;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;
using TagHierarchyManager.UI.Views;

namespace TagHierarchyManager.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // TODO consider moving editor-related functionality to a separate ViewModel.
    internal TagDatabase Database;

    [ObservableProperty] private HierarchyTreeViewModel _hierarchyTreeViewModel;
    
    [ObservableProperty] private SearchViewModel _searchViewModel;

    [ObservableProperty] private bool _isDbLoaded;

    private bool _isSwitching;

    // Since multiple view models will be using this tag, best to store it here as the authoritative source.
    private TagItemViewModel? _selectedTag;

    [ObservableProperty] private string _statusBlockText = Resources.StatusBlockReady;

    [ObservableProperty] private ObservableCollection<TagItemViewModel> _topLevelTags = [];

    [ObservableProperty] private bool _unsavedChanges;

    public int TotalTags => this.Database?.Tags.Count ?? 0;

    public string WindowTitle =>
        this.IsDbLoaded
            ? string.Format(Resources.TitleWithDatabase, this.Database.Name)
            : Resources.Title;

    public TagItemViewModel? SelectedTag
    {
        get => this._selectedTag;
        set
        {
            if (this._selectedTag == value || this._isSwitching) return;
            if (this._selectedTag != null && this.UnsavedChanges)
            {
                _ = this.HandleTagSwitchAsync(this._selectedTag, value);
            }
            else
            {
                this._selectedTag = value;
                this.HierarchyTreeViewModel.SelectedTag = value;
                this._selectedTag?.BeginEdit();
                this.UnsavedChanges = false;
                this.OnPropertyChanged();
            }
        }
    }

    public async Task CreateNewDatabase(string filePath)
    {
        this.IsDbLoaded = false;
        TagDatabase db = new();
        db.InitialisationComplete += this.TagDatabase_OnInitalisationComplete;
        // overwrite is set to true here for now since the OS should handle the overwrite request.
        // will need to remove once Terminal.Gui is replaced.
        await db.CreateAsync(filePath, true);
    }

    public async Task ExportAsync(string path)
    {
        var exporter = PickExporterFromFileExt(path);
        this.StatusBlockText = "Exporting...";
        var exportContent = exporter.ExportDatabase(this.Database);
        await File.WriteAllTextAsync(path, exportContent);
        this.StatusBlockText = "Export complete.";
    }

    public async Task LoadDatabase(string filePath)
    {
        this.IsDbLoaded = false;
        TagDatabase db = new();
        db.InitialisationComplete += this.TagDatabase_OnInitalisationComplete;
        await db.LoadAsync(filePath);
    }

    public void NewTag()
    {
        this.SelectedTag = new TagItemViewModel(
            new Tag
            {
                Name = string.Empty,
                IsTopLevel = true
            }
        );
        this.SelectedTag.BeginEdit();
        this.UnsavedChanges = true;
    }

    public async Task SaveSelectedTagAsync()
    {
        if (this.SelectedTag is null || this.Database is null) return;

        this.SelectedTag.CommitEdit();
        await this.Database.WriteTagToDatabase(this.SelectedTag.Tag);
        this.StatusBlockText = $"Successfully saved tag {this.SelectedTag.Name}";
        this.UnsavedChanges = false;
    }

    public async Task<bool?> ShowNullableBoolDialog(Window dialog)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        var mainWindow = desktop.MainWindow;
        var result = await dialog.ShowDialog<bool?>(mainWindow);
        return result;
    }

    public void StartSearch(string searchQuery, TagDatabaseSearchMode mode, bool searchAliases)
    {
        this.SearchViewModel.Search(searchQuery, mode, searchAliases);
    }

    public async Task StartTagDeletion()
    {
        var result = await this.ShowNullableBoolDialog(new DeleteTagDialog());
        if (result == null)
            return;

        if (result == true) await this.DeleteSelectedTagAsync();
    }

    private static IExporter PickExporterFromFileExt(string path)
    {
        var fileExt = Path.GetExtension(path);

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (fileExt == FileTypes.MusicBeeTagHierarchyTemplate.FileExtension) return new MusicBeeTagHierarchyExporter();

        throw new NotSupportedException($"File extension '{fileExt}' is not supported.");
    }

    private async Task DeleteSelectedTagAsync()
    {
        if (this.SelectedTag is null || this.Database is null) return;
        await this.Database.DeleteTag(this.SelectedTag.Tag.Id);
        this._selectedTag = null;
        this.HierarchyTreeViewModel.SelectedTag = null;
        this.OnPropertyChanged(nameof(this.SelectedTag));
    }

    private async Task HandleTagSwitchAsync(TagItemViewModel? oldTag, TagItemViewModel? newTag)
    {
        this._isSwitching = true;
        try
        {
            var result = await this.ShowNullableBoolDialog(new UnsavedChangesDialog());

            if (result == null)
            {
                this._selectedTag = oldTag;
                this.HierarchyTreeViewModel.SelectedTag = oldTag;
                this.OnPropertyChanged(nameof(this.SelectedTag));
                return;
            }

            if (result == true)
                await this.SaveSelectedTagAsync();

            this._selectedTag = newTag;
            this.HierarchyTreeViewModel.SelectedTag = newTag;
            this._selectedTag?.BeginEdit();
            this.OnPropertyChanged(nameof(this.SelectedTag));
            this.UnsavedChanges = false;
        }
        finally
        {
            this._isSwitching = false;
        }
    }

    private void TagDatabase_OnInitalisationComplete(object sender, EventArgs e)
    {
        if (sender is not TagDatabase db) return;
        Dispatcher.UIThread.Post(async () =>
        {
            (this.HierarchyTreeViewModel as IDisposable)?.Dispose();
            (this.SearchViewModel as IDisposable)?.Dispose();
            
            if (this.Database != null)
            {
                this.Database.TagAdded -= this.TagDatabase_TagAdded;
                this.Database.TagDeleted -= this.TagDatabase_TagDeleted;
            }

            this.Database = db;
            this.IsDbLoaded = true;
            this.HierarchyTreeViewModel = new HierarchyTreeViewModel(this);
            this.SearchViewModel = new SearchViewModel(this);
            this.Database.TagAdded += this.TagDatabase_TagAdded;
            this.Database.TagDeleted += this.TagDatabase_TagDeleted;
            await this.HierarchyTreeViewModel.InitializeAsync();
            this.OnPropertyChanged(nameof(this.TotalTags));
            this.OnPropertyChanged(nameof(this.WindowTitle));
            this.Database.InitialisationComplete -= this.TagDatabase_OnInitalisationComplete;
            this.UnsavedChanges = false;
            this.StatusBlockText = string.Format(Resources.StatusBlockDbLoadSuccessful, this.Database.Name);
        });
        Debug.WriteLine($"Database loaded on UI - name: {db.Name}, version: {db.Version}");
    }

    private void TagDatabase_TagAdded(object? sender, Tag _)
    {
        this.OnPropertyChanged(nameof(this.TotalTags));
    }

    private void TagDatabase_TagDeleted(object? sender, (int id, string name) _)
    {
        this.OnPropertyChanged(nameof(this.TotalTags));
    }
}