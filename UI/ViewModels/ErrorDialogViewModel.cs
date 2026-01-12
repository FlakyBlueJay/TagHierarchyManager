using Avalonia.Controls;
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

    public void ShowDialog(Window window)
    {
        ErrorDialog error = new()
        {
            DataContext = this,
            Title = Resources.ErrorDialogTitle
        };
        error.ShowDialog(window);
    }
}