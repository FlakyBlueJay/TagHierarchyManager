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
    internal TagDatabase? Database;

    [ObservableProperty] private HierarchyTreeViewModel? _hierarchyTreeViewModel;
    
    [ObservableProperty] private SearchViewModel? _searchViewModel;

    [ObservableProperty] private bool _isDbEnabled;

    private bool _isSwitching;

    // Since multiple view models will be using this tag, best to store it here as the authoritative source.
    private TagItemViewModel? _selectedTag;

    [ObservableProperty] private string _statusBlockText = Resources.StatusBlockReady;

    [ObservableProperty] private ObservableCollection<TagItemViewModel> _topLevelTags = [];

    [ObservableProperty] private bool _unsavedChanges;

    public int TotalTags => this.Database?.Tags.Count ?? 0;

    public string WindowTitle =>
        this.IsDbEnabled
            ? string.Format(Resources.TitleWithDatabase, this.Database?.Name)
            : Resources.Title;

    public TagItemViewModel? SelectedTag
    {
        get => this._selectedTag;
        set
        {
            if (this._selectedTag == value || this._isSwitching) return;
            if (this._isDbEnabled == false && value == null) this._selectedTag = value;
            if (this._selectedTag != null && this.UnsavedChanges)
            {
                _ = this.HandleTagSwitchAsync(this._selectedTag, value);
            }
            else
            {
                this._selectedTag = value;
                this.HierarchyTreeViewModel?.SelectedTag = value;
                this._selectedTag?.BeginEdit();
                this.UnsavedChanges = false;
                this.OnPropertyChanged();
            }
        }
    }

    public async Task CreateNewDatabase(string filePath)
    {
        if (this.Database != null)
            this.UninitialiseDatabase();

        try
        {
            TagDatabase db = new();
            db.InitialisationComplete += this.TagDatabase_OnInitalisationComplete;
            // overwrite is set to true here for now since the OS should handle the overwrite request.
            // will need to remove once Terminal.Gui is fully replaced.
            await Task.Run(() => db.CreateAsync(filePath, true));
        }
        catch (Exception ex)
        {
            this.UninitialiseDatabase();
            this.ShowErrorDialog(ex.Message);
        }
        
    }

    public async Task ExportAsync(string path)
    {
        this.IsDbEnabled = false;
        try
        {
            if (this.Database is null || string.IsNullOrWhiteSpace(path)) return;
            
            var exporter = PickExporterFromFileExt(path);
            this.StatusBlockText = Resources.StatusBlockExportInProgress;
            var exportContent = await Task.Run(() => exporter.ExportDatabase(this.Database!));
            await File.WriteAllTextAsync(path, exportContent);
            this.IsDbEnabled = true;
            this.StatusBlockText = string.Format(Resources.StatusBlockExportSuccessful, path);
        }
        catch (Exception ex)
        {
            this.IsDbEnabled = true;
            this.ShowErrorDialog(ex.Message);
        }

    }

    public async Task LoadDatabase(string filePath)
    {
        try
        {
            if (this.Database != null)
                this.UninitialiseDatabase();
            
            TagDatabase db = new();
            db.InitialisationComplete += this.TagDatabase_OnInitalisationComplete;
            await Task.Run(() => db.LoadAsync(filePath));
        }
        catch (Exception ex)
        {
            this.UninitialiseDatabase();
            this.ShowErrorDialog(ex.Message);
        }
        
    }

    public void NewTag()
    {
        this.SelectedTag = new TagItemViewModel(
            new Tag
            {
                Name = string.Empty,
                IsTopLevel = true,
                TagBindings = this.Database!.DefaultTagBindings
            }
        );
        this.SelectedTag.BeginEdit();
        this.UnsavedChanges = true;
    }

    public async Task SaveSelectedTagAsync()
    {
        if (this.SelectedTag is null || this.Database is null) return;

        try
        {
            this.SelectedTag.CommitEdit();
            await this.Database.WriteTagToDatabase(this.SelectedTag.Tag);
            this.StatusBlockText = string.Format(Assets.Resources.StatusBlockTagSaveSuccessful, this.SelectedTag.Name);
            this.UnsavedChanges = false;
        }
        catch (Exception ex)
        {
            this.ShowErrorDialog(ex.Message);
        }
    }

    public void ShowErrorDialog(string message)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;
        
        var mainWindow = desktop.MainWindow;
        var error = new ErrorDialogViewModel(message);
        error.ShowDialog(mainWindow);
    }

    public async Task<bool?> ShowNullableBoolDialog(Window dialog)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        var mainWindow = desktop.MainWindow;
        var result = await dialog.ShowDialog<bool?>(mainWindow!);
        return result;
    }

    public void StartSearch(string searchQuery, TagDatabaseSearchMode mode, bool searchAliases)
    {
        this.SearchViewModel?.Search(searchQuery, mode, searchAliases);
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
        try
        {
            await this.Database.DeleteTag(this.SelectedTag.Tag.Id);
            this._selectedTag = null;
            this.HierarchyTreeViewModel?.SelectedTag = null;
            this.OnPropertyChanged(nameof(this.SelectedTag));
        }
        catch (Exception ex)
        {
            this.ShowErrorDialog(ex.Message);
        }
        
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
                this.HierarchyTreeViewModel?.SelectedTag = oldTag;
                this.OnPropertyChanged(nameof(this.SelectedTag));
                return;
            }

            if (result == true)
                await this.SaveSelectedTagAsync();

            this._selectedTag = newTag;
            this.HierarchyTreeViewModel?.SelectedTag = newTag;
            this._selectedTag?.BeginEdit();
            this.OnPropertyChanged(nameof(this.SelectedTag));
            this.UnsavedChanges = false;
        }
        finally
        {
            this._isSwitching = false;
        }
    }

    private void TagDatabase_OnInitalisationComplete(object? sender, EventArgs e)
    {
        if (sender is not TagDatabase db) return;
        Dispatcher.UIThread.Post(async void () =>
        {
            try
            {
                (this.HierarchyTreeViewModel as IDisposable)?.Dispose();
                (this.SearchViewModel as IDisposable)?.Dispose();
            
                if (this.Database != null)
                {
                    this.Database.TagAdded -= this.TagDatabase_TagAdded;
                    this.Database.TagDeleted -= this.TagDatabase_TagDeleted;
                }

                this.Database = db;
                this.SelectedTag = null;
                this.IsDbEnabled = true;
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
            }
            catch (Exception ex)
            {
                this.UninitialiseDatabase();
                this.ShowErrorDialog(ex.Message);
            }
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

    private void UninitialiseDatabase()
    {
        if (this.Database == null) return;
        this.Database.TagAdded -= this.TagDatabase_TagAdded;
        this.Database.TagDeleted -= this.TagDatabase_TagDeleted;
        this.Database = null;
        this.IsDbEnabled = false;
        this.HierarchyTreeViewModel = null;
        this.SearchViewModel = null;
        this.OnPropertyChanged(nameof(this.TotalTags));
        this.OnPropertyChanged(nameof(this.WindowTitle));
    }
}