using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class ImportDialogViewModel : ViewModelBase
{
    public event Action? RequestClose;
    
    public ImportDialogViewModel(MainWindowViewModel mainWindow)
    {
        this.MainWindow = mainWindow;
    }
    
    public string VisibleTemplateFilePath => 
        !string.IsNullOrWhiteSpace(TemplateFilePath) ? this.TemplateFilePath : Resources.ImportNoFilePicked;
    
    public string VisibleDatabaseFilePath => 
        !string.IsNullOrWhiteSpace(DatabaseFilePath) ? this.DatabaseFilePath : Resources.ImportNoFilePicked;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibleTemplateFilePath))]
    [NotifyPropertyChangedFor(nameof(BothFilesSelected))]
    private string _templateFilePath;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibleDatabaseFilePath))]
    [NotifyPropertyChangedFor(nameof(BothFilesSelected))]
    private string _databaseFilePath;

    [ObservableProperty] private string _importStatus = string.Empty;

    public bool BothFilesSelected => !string.IsNullOrWhiteSpace(TemplateFilePath) && !string.IsNullOrWhiteSpace(DatabaseFilePath);
    
    private MainWindowViewModel MainWindow { get; }

    public async Task InitiateImport()
    {
        try
        {
            ImportStatus = Assets.Resources.ImportStatusInProgress;
            await this.MainWindow.CreateNewDatabase(DatabaseFilePath, TemplateFilePath);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ImportStatus = Assets.Resources.ImportStatusFailed;
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }
}