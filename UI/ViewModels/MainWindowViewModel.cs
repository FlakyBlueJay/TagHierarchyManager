using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagHierarchyManager.UI.Assets;
using TagHierarchyManager.UI.Views;

namespace TagHierarchyManager.UI.ViewModels;

// things have improved here but i will want to separate some more stuff one day.
// such as putting search functions and tag editor functions in their own viewmodels.
public partial class MainWindowViewModel : ViewModelBase
{
    internal readonly Func<List<int>, List<string>> GetParentNamesById;
    [ObservableProperty] private HierarchyTreeViewModel? _hierarchyTreeViewModel;

    [ObservableProperty] private bool _isDbEnabled;
    private bool _isSwitching;

    [ObservableProperty] private SearchViewModel? _searchViewModel;
    private TagItemViewModel? _selectedTag;

    [ObservableProperty] private int _selectedTagId;

    [ObservableProperty] private string _statusBlockText = Resources.StatusBlockReady;

    [ObservableProperty] private TagDatabaseService _tagDatabaseService;
    [ObservableProperty] private TagEditorViewModel? _tagEditorViewModel;

    [ObservableProperty] private bool _unsavedChanges;

    public MainWindowViewModel(TagDatabaseService tagDatabaseService)
    {
        this.TagDatabaseService = tagDatabaseService;
        this.TagDatabaseService.InitialisationComplete += this.TagDatabaseService_OnInitalisationComplete;
        this.TagDatabaseService.TagsWritten += this.TagDatabaseService_TagsWritten;
        this.TagDatabaseService.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(this.TagDatabaseService.TagCount))
                this.OnPropertyChanged(nameof(this.TotalTags));
        };
        this.GetParentNamesById = tagDatabaseService.GetParentNamesByIds;
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
            if (this._selectedTag == value || this.TagEditorViewModel is null || this._isSwitching) return;
            if (value is null)
            {
                this._selectedTag = null;
                this.OnPropertyChanged();
                this.TagEditorViewModel.SelectedTag = null;
                return;
            }

            if (this._selectedTag is not null && this.TagEditorViewModel.UnsavedChanges)
            {
                _ = this.HandleTagSwitchAsync(this._selectedTag, value);
                return;
            }

            if (this._selectedTag == value) return;
            value.RefreshParentsString();
            this._selectedTag = value;
            this.OnPropertyChanged();
            this.TagEditorViewModel?.SelectedTag = value;
        }
    }


    public async Task<bool?> ShowUnsavedChangesDialog()
    {
        if (!this.UnsavedChanges) return true;
        var result = await this.ShowNullableBoolDialog(new UnsavedChangesDialog());
        return result;
    }

    internal void ShowErrorDialog(string message)
    {
        var error = new ErrorDialogViewModel(message);
        error.ShowDialog();
    }

    internal async Task<bool?> ShowNullableBoolDialog(Window dialog)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        var mainWindow = desktop.MainWindow;
        var result = await dialog.ShowDialog<bool?>(mainWindow!);
        return result;
    }

    [RelayCommand]
    internal async Task StartTagDeletionAsync(int id)
    {
        var result = await this.ShowNullableBoolDialog(new DeleteTagDialog());
        switch (result)
        {
            case null:
            case false:
                return;
            case true:
                await this.DeleteTagAsync(id);
                break;
        }
    }

    private void CloseDatabase()
    {
        this.TagEditorViewModel?.SelectedTag = null;
        this.UnhookTagEditor(this.TagEditorViewModel);
        this.IsDbEnabled = false;
        this.TagEditorViewModel = null;
        this.HierarchyTreeViewModel = null;
        this.SearchViewModel = null;
        this.OnPropertyChanged(nameof(this.TotalTags));
        this.OnPropertyChanged(nameof(this.WindowTitle));
        this.TagDatabaseService.CloseDatabase();
    }

    private async Task DeleteTagAsync(int id)
    {
        var oldSelectedTag = this.TagEditorViewModel?.SelectedTag;
        if (!this.TagDatabaseService.IsDatabaseOpen) return;
        try
        {
            await this.TagDatabaseService.DeleteTag(id);
            if (oldSelectedTag is not null && oldSelectedTag.Tag.Id == id)
            {
                this.HierarchyTreeViewModel?.SelectedTag = null;
                this.TagEditorViewModel?.SelectedTag = null;
                this.OnPropertyChanged(nameof(this.TagEditorViewModel.SelectedTag));
            }
        }
        catch (Exception ex)
        {
            this.TagEditorViewModel?.SelectedTag = oldSelectedTag;
            this.ShowErrorDialog(ex.Message);
        }
    }

    private async Task HandleTagSwitchAsync(TagItemViewModel? oldTag, TagItemViewModel? newTag)
    {
        if (this.TagEditorViewModel is null) return;
        this._isSwitching = true;
        try
        {
            var result = await this.ShowNullableBoolDialog(new UnsavedChangesDialog());

            switch (result)
            {
                case null:
                    this._selectedTag = oldTag;
                    // this.HierarchyTreeViewModel?.SelectedTag = oldTag;
                    this.OnPropertyChanged(nameof(this.SelectedTag));
                    return;
                case false:
                    break;
                case true:
                    await this.TagEditorViewModel.SaveSelectedTagAsync();
                    break;
            }

            // do not set this to the generated public variable from Avalonia.
            // OnPropertyChanged must be handled manually here to prevent tag changes firing
            // twice.
            this._selectedTag = newTag;
            this.OnPropertyChanged(nameof(this.SelectedTag));
            this.TagEditorViewModel.SelectedTag = newTag;
            this.UnsavedChanges = false;
        }
        finally
        {
            this._isSwitching = false;
        }
    }

    private void HookTagEditor(TagEditorViewModel tagEditor)
    {
        tagEditor.PropertyChanged += this.TagEditorOnPropertyChanged;
        this.UnsavedChanges = tagEditor.UnsavedChanges;
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

    [RelayCommand]
    private void ShowBulkAddDialog()
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
    private void ShowDatabaseSettings()
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
    private void ShowImportDialog()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var dialog = new ImportDialog
        {
            DataContext = new ImportDialogViewModel(this)
        };

        dialog.ShowDialog(desktop.MainWindow!);
    }

    [RelayCommand]
    private void ShowRecentsWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var dialog = new RecentsWindow
        {
            DataContext = new RecentsViewModel(this)
        };

        dialog.ShowDialog(desktop.MainWindow!);
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
    private void StartNewTag()
    {
        this.TagEditorViewModel?.NewTag();
    }

    [RelayCommand]
    private async Task StartTagSaveAsync()
    {
        if (this.TagEditorViewModel is null) return;
        await this.TagEditorViewModel.SaveSelectedTagAsync();
    }

    private void TagDatabaseService_OnInitalisationComplete(object? sender, EventArgs e)
    {
        if (sender is not TagDatabaseService _) return;
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                (this.HierarchyTreeViewModel as IDisposable)?.Dispose();
                (this.SearchViewModel as IDisposable)?.Dispose();
                (this.TagEditorViewModel as IDisposable)?.Dispose();

                this.HierarchyTreeViewModel = new HierarchyTreeViewModel(this);
                this.SearchViewModel = new SearchViewModel(this);
                this.TagEditorViewModel = new TagEditorViewModel(this);

                this.HookTagEditor(this.TagEditorViewModel);

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

    private void TagDatabaseService_TagsWritten(object? sender, TagDatabaseService.TagWriteResult result)
    {
        this.OnPropertyChanged(nameof(this.TotalTags));
    }

    private void TagEditorOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(this.TagEditorViewModel.UnsavedChanges)) return;
        if (sender is not TagEditorViewModel tagEditor) return;
        this.UnsavedChanges = tagEditor.UnsavedChanges;
    }

    private void UnhookTagEditor(TagEditorViewModel? tagEditor)
    {
        if (tagEditor is null) return;
        tagEditor.PropertyChanged -= this.TagEditorOnPropertyChanged;
    }
}