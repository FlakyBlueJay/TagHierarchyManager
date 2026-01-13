using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class ImportDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibleDatabaseFilePath))]
    [NotifyPropertyChangedFor(nameof(BothFilesSelected))]
    private string _databaseFilePath;

    [ObservableProperty] private string _importStatus = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibleTemplateFilePath))]
    [NotifyPropertyChangedFor(nameof(BothFilesSelected))]
    private string _templateFilePath;

    public ImportDialogViewModel(MainWindowViewModel mainWindow)
    {
        this.MainWindow = mainWindow;
    }

    public event Action? RequestClose;

    public bool BothFilesSelected => !string.IsNullOrWhiteSpace(this.TemplateFilePath) &&
                                     !string.IsNullOrWhiteSpace(this.DatabaseFilePath);

    public string VisibleDatabaseFilePath =>
        !string.IsNullOrWhiteSpace(this.DatabaseFilePath) ? this.DatabaseFilePath : Resources.ImportNoFilePicked;

    public string VisibleTemplateFilePath =>
        !string.IsNullOrWhiteSpace(this.TemplateFilePath) ? this.TemplateFilePath : Resources.ImportNoFilePicked;

    private MainWindowViewModel MainWindow { get; }

    public async Task InitiateImport()
    {
        try
        {
            this.ImportStatus = Resources.ImportStatusInProgress;
            await this.MainWindow.CreateNewDatabase(this.DatabaseFilePath, this.TemplateFilePath);
            this.RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            this.ImportStatus = Resources.ImportStatusFailed;
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }
}