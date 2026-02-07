using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class ImportDialogViewModel(MainWindowViewModel mainWindow) : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibleDatabaseFilePath))]
    [NotifyPropertyChangedFor(nameof(BothFilesSelected))]
    private string _databaseFilePath = string.Empty;

    [ObservableProperty] private string _importStatus = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibleTemplateFilePath))]
    [NotifyPropertyChangedFor(nameof(BothFilesSelected))]
    private string _templateFilePath = string.Empty;

    public event Action? RequestClose;

    public bool BothFilesSelected => !string.IsNullOrWhiteSpace(this.TemplateFilePath) &&
                                     !string.IsNullOrWhiteSpace(this.DatabaseFilePath);

    public string VisibleDatabaseFilePath =>
        !string.IsNullOrWhiteSpace(this.DatabaseFilePath) ? this.DatabaseFilePath : Resources.ImportNoFilePicked;

    public string VisibleTemplateFilePath =>
        !string.IsNullOrWhiteSpace(this.TemplateFilePath) ? this.TemplateFilePath : Resources.ImportNoFilePicked;

    private MainWindowViewModel MainWindow { get; } = mainWindow;

    [RelayCommand]
    private async Task InitiateImport()
    {
        try
        {
            if (string.IsNullOrEmpty(this.DatabaseFilePath) || string.IsNullOrEmpty(this.TemplateFilePath)) return;

            this.ImportStatus = Resources.ImportStatusInProgress;
            await this.MainWindow.TagDatabaseService.CreateNewDatabase(this.DatabaseFilePath, this.TemplateFilePath);
            this.RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            this.ImportStatus = Resources.ImportStatusFailed;
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }

    [RelayCommand]
    private async Task ShowDatabaseBrowseDialog()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow is null) return;

            var userWantsToSave = await this.MainWindow.ShowUnsavedChangesDialog();
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
            this.DatabaseFilePath = path;
        }
        catch (Exception ex)
        {
            this.TemplateFilePath = string.Empty;
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }

    [RelayCommand]
    private async Task ShowTemplateBrowseDialog()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow is null) return;

            var userWantsToSave = await this.MainWindow.ShowUnsavedChangesDialog();
            if (userWantsToSave is null) return;

            var storageProvider = desktop.MainWindow?.StorageProvider;
            if (storageProvider is null) return;

            var files = await storageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = Resources.DialogTitleChooseImportTemplate,
                    FileTypeFilter = [Common.MusicBeeTagHierarchy]
                });
            if (files.Count == 0) return;
            var path = files[0].TryGetLocalPath();
            if (path == null) return;
            this.TemplateFilePath = path;
        }
        catch (Exception ex)
        {
            this.TemplateFilePath = string.Empty;
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }
}