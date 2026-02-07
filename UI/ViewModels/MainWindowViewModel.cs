using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagHierarchyManager.Common;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;
using TagHierarchyManager.UI.Views;

namespace TagHierarchyManager.UI.ViewModels;

// things have improved here but i will want to separate some more stuff one day.
// such as putting search functions and tag editor functions in their own viewmodels.
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private HierarchyTreeViewModel? _hierarchyTreeViewModel;

    [ObservableProperty] private bool _isDbEnabled;


    private bool _isSwitching;

    [ObservableProperty] private SearchViewModel? _searchViewModel;

    // Since multiple view models will be using this tag, best to store it here as the authoritative source.
    private TagItemViewModel? _selectedTag;

    [ObservableProperty] private string _statusBlockText = Resources.StatusBlockReady;

    [ObservableProperty] private TagDatabaseService _tagDatabaseService;

    [ObservableProperty] private bool _unsavedChanges;

    public MainWindowViewModel(TagDatabaseService tagDatabaseService)
    {
        this.TagDatabaseService = tagDatabaseService;
        this.TagDatabaseService.InitialisationComplete += this.TagDatabaseService_OnInitalisationComplete;
        this.TagDatabaseService.TagAdded += this.TagDatabaseService_TagAdded;
        this.TagDatabaseService.TagDeleted += this.TagDatabaseService_TagDeleted;
        this.TagDatabaseService.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(this.TagDatabaseService.TagCount))
            {
                this.OnPropertyChanged(nameof(this.TotalTags));
            }
        };
    }

    public int TotalTags => this.TagDatabaseService.TagCount;

    public string WindowTitle =>
        this.TagDatabaseService.IsDatabaseOpen
            ? string.Format(Resources.TitleWithDatabase, this.TagDatabaseService.DatabaseName)
            : Resources.Title;

    public TagItemViewModel? SelectedTag
    {
        get => this._selectedTag;
        set
        {
            if (this._selectedTag == value || this._isSwitching) return;
            if (value == null) this._selectedTag = value;
            if (this._selectedTag != null && this.UnsavedChanges)
            {
                _ = this.HandleTagSwitchAsync(this._selectedTag, value);
            }
            else
            {
                this._selectedTag?.UserEditedTag -= this.OnUserEditedTag;
                this._selectedTag = value;
                this.HierarchyTreeViewModel?.SelectedTag = value;
                this._selectedTag?.BeginEdit();
                this.OnPropertyChanged();
                this.UnsavedChanges = false;
                this._selectedTag?.UserEditedTag += this.OnUserEditedTag;
            }
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task NewTag()
    {
        var userWantsToSave = await this.ShowUnsavedChangesDialog();
        if (userWantsToSave is null) return;

        this.SelectedTag = new TagItemViewModel(
            new Tag
            {
                Name = string.Empty,
                IsTopLevel = true,
                TagBindings = this.TagDatabaseService.DefaultTagBindings
            }
        );
        this.UnsavedChanges = true;
    }

    [RelayCommand]
    public async Task SaveSelectedTagAsync()
    {
        if (this.SelectedTag is null || !this.TagDatabaseService.IsDatabaseOpen || !this.IsDbEnabled) return;

        this.SelectedTag.CommitEdit();
        await this.TagDatabaseService.WriteTagsToDatabase([this.SelectedTag.Tag]);
        this.SelectedTag.RefreshParentsString();
        this.StatusBlockText = string.Format(Resources.StatusBlockTagSaveSuccessful, this.SelectedTag.Name);
        this.UnsavedChanges = false;
    }

    [RelayCommand]
    public void ShowBulkAddDialog()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var dialog = new BulkAddWindow
        {
            DataContext = new BulkAddViewModel(this)
        };

        dialog.ShowDialog(desktop.MainWindow!);
    }

    [RelayCommand]
    public void ShowDatabaseSettings()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var dialog = new DatabaseSettingsWindow
        {
            DataContext = new DatabaseSettingsViewModel(this)
        };

        dialog.ShowDialog(desktop.MainWindow!);
    }

    [RelayCommand]
    public void ShowImportDialog()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var dialog = new ImportDialog
        {
            DataContext = new ImportDialogViewModel(this)
        };

        dialog.ShowDialog(desktop.MainWindow!);
    }

    public async Task<bool?> ShowNullableBoolDialog(Window dialog)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        var mainWindow = desktop.MainWindow;
        var result = await dialog.ShowDialog<bool?>(mainWindow!);
        return result;
    }

    public async Task<bool?> ShowUnsavedChangesDialog()
    {
        if (!this.UnsavedChanges) return true;
        var result = await this.ShowNullableBoolDialog(new UnsavedChangesDialog());
        return result;
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
            this.SearchViewModel?.Search(query, mode, searchAliases);
        }
        catch (Exception ex)
        {
            this.ShowErrorDialog(ex.Message);
        }
    }

    [RelayCommand]
    private async Task CancelTagEditAsync()
    {
        if (this.UnsavedChanges)
        {
            var userWantsToOverwrite = await this.ShowNullableBoolDialog(new UnsavedCancelDialog());
            if (userWantsToOverwrite is not null && (bool)!userWantsToOverwrite) return;
        }

        this.SelectedTag?.BeginEdit();
        this.UnsavedChanges = false;
    }

    private void CloseDatabase()
    {
        this.SelectedTag = null;
        this.IsDbEnabled = false;
        this.HierarchyTreeViewModel = null;
        this.SearchViewModel = null;
        this.OnPropertyChanged(nameof(this.TotalTags));
        this.OnPropertyChanged(nameof(this.WindowTitle));
        this.TagDatabaseService.CloseDatabase();
    }

    private async Task DeleteSelectedTagAsync()
    {
        var oldTag = this.SelectedTag;
        if (this.SelectedTag is null || !this.TagDatabaseService.IsDatabaseOpen) return;
        try
        {
            await this.TagDatabaseService.DeleteTag(this.SelectedTag.Tag.Id);
            this._selectedTag = null;
            this.HierarchyTreeViewModel?.SelectedTag = null;
            this.OnPropertyChanged(nameof(this.SelectedTag));
        }
        catch (Exception ex)
        {
            this._selectedTag = oldTag;
            // TODO invoke deleting/delete error so hierarchy tree can recover
            this.ShowErrorDialog(ex.Message);
        }
    }

    private async Task HandleTagSwitchAsync(TagItemViewModel? oldTag, TagItemViewModel? newTag)
    {
        this._isSwitching = true;
        try
        {
            var result = await this.ShowNullableBoolDialog(new UnsavedChangesDialog());

            switch (result)
            {
                case null:
                    this._selectedTag = oldTag;
                    this.HierarchyTreeViewModel?.SelectedTag = oldTag;
                    this.OnPropertyChanged(nameof(this.SelectedTag));
                    return;
                case false:
                    break;
                case true:
                    await this.SaveSelectedTagAsync();
                    break;
            }

            // do not set this to the generated public variable from Avalonia.
            // OnPropertyChanged must be handled manually here to prevent tag changes firing
            // twice.
            this._selectedTag = newTag;
            this.HierarchyTreeViewModel?.SelectedTag = newTag;
            this._selectedTag?.BeginEdit();
            this._selectedTag?.UserEditedTag += this.OnUserEditedTag;
            this.OnPropertyChanged(nameof(this.SelectedTag));
            this.UnsavedChanges = false;
        }
        finally
        {
            this._isSwitching = false;
        }
    }

    [RelayCommand]
    private async Task NewDatabaseAsync()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow is null) return;

            var userWantsToSave = await this.ShowUnsavedChangesDialog();
            if (userWantsToSave is null) return;

            var storageProvider = desktop.MainWindow?.StorageProvider;
            if (storageProvider is null) return;

            var file = await storageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = Resources.DialogTitleSaveDatabaseAs,
                    FileTypeChoices = [Common.TagDatabaseFileType]
                });
            var path = file?.TryGetLocalPath();
            if (path == null) return;
            await this.TagDatabaseService.CreateNewDatabase(path);
        }
        catch (Exception ex)
        {
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }

    private void OnUserEditedTag(object? sender, EventArgs e)
    {
        this.UnsavedChanges = true;
    }

    [RelayCommand]
    private async Task OpenDatabase()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow is null) return;

            var userWantsToSave = await this.ShowUnsavedChangesDialog();
            if (userWantsToSave is null) return;

            var storageProvider = desktop.MainWindow?.StorageProvider;
            if (storageProvider is null) return;

            var files = await storageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = Resources.DialogTitleOpenDatabase,
                    FileTypeFilter = [Common.TagDatabaseFileType]
                });
            if (files.Count == 0) return;
            var path = files[0].TryGetLocalPath();
            if (path == null) return;
            await this.TagDatabaseService.LoadDatabase(path);
        }
        catch (Exception ex)
        {
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }

    private void ShowErrorDialog(string message)
    {
        var error = new ErrorDialogViewModel(message);
        error.ShowDialog();
    }

    [RelayCommand]
    private async Task StartExportAsync()
    {
        if (!this.TagDatabaseService.IsDatabaseOpen ||
            Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is null) return;

        var userWantsToSave = await this.ShowUnsavedChangesDialog();
        if (userWantsToSave is null) return;

        var storageProvider = desktop.MainWindow?.StorageProvider;
        if (storageProvider is null) return;

        try
        {
            var file = await storageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = Resources.DialogTitleExportTagDatabase,
                    FileTypeChoices = [Common.MusicBeeTagHierarchy],
                    SuggestedFileName = this.TagDatabaseService.DatabaseName
                });
            var path = file?.TryGetLocalPath();
            if (path == null) return;
            this.IsDbEnabled = false;
            this.StatusBlockText = Resources.StatusBlockExportInProgress;
            await this.TagDatabaseService.ExportAsync(path);
            this.IsDbEnabled = true;
            this.StatusBlockText = string.Format(Resources.StatusBlockExportSuccessful, path);
        }
        catch (Exception ex)
        {
            this.IsDbEnabled = true;
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }

    [RelayCommand]
    private async Task StartTagDeletionAsync()
    {
        if (this.SelectedTag is null || this.SelectedTag.HasChildren) return;
        var result = await this.ShowNullableBoolDialog(new DeleteTagDialog());
        switch (result)
        {
            case null:
            case false:
                return;
            case true:
                await this.DeleteSelectedTagAsync();
                break;
        }
    }

    private void TagDatabaseService_OnInitalisationComplete(object? sender, EventArgs e)
    {
        if (sender is not TagDatabaseService _) return;
        Dispatcher.UIThread.Post(async void () =>
        {
            try
            {
                (this.HierarchyTreeViewModel as IDisposable)?.Dispose();
                (this.SearchViewModel as IDisposable)?.Dispose();

                this.SelectedTag = null;

                this.HierarchyTreeViewModel = new HierarchyTreeViewModel(this);
                this.SearchViewModel = new SearchViewModel(this);

                await this.HierarchyTreeViewModel.InitializeAsync();
                this.OnPropertyChanged(nameof(this.TotalTags));
                this.OnPropertyChanged(nameof(this.WindowTitle));
                this.IsDbEnabled = true;

                this.UnsavedChanges = false;
                this.StatusBlockText = string.Format(Resources.StatusBlockDbLoadSuccessful,
                    this.TagDatabaseService.DatabaseName);
            }
            catch (Exception ex)
            {
                this.CloseDatabase();
                this.ShowErrorDialog(ex.Message);
            }
        });
    }

    private void TagDatabaseService_TagAdded(object? sender, Tag _)
    {
        this.OnPropertyChanged(nameof(this.TotalTags));
        this.SelectedTag!.SyncId();
    }

    private void TagDatabaseService_TagDeleted(object? sender, (int id, string name) _)
    {
        this.OnPropertyChanged(nameof(this.TotalTags));
    }
}