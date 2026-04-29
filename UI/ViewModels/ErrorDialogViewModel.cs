using CommunityToolkit.Mvvm.ComponentModel;

namespace TagHierarchyManager.UI.ViewModels;

public partial class ErrorDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _errorMessage;

    public ErrorDialogViewModel(string message)
    {
        this.ErrorMessage = message;
    }
}