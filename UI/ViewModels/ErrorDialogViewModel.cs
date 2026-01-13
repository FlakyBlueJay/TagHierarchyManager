using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.UI.Assets;
using TagHierarchyManager.UI.Views;

namespace TagHierarchyManager.UI.ViewModels;

public partial class ErrorDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _errorMessage;
    
    public ErrorDialogViewModel(string message)
    {
        this.ErrorMessage = message;
    }
    
    public void ShowDialog()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;
        
        ErrorDialog error = new()
        {
            DataContext = this,
            Title = Resources.ErrorDialogTitle
        };
        var ownerWindow = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
        error.ShowDialog(ownerWindow);
    }
}