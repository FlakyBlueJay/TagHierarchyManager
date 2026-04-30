using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using TagHierarchyManager.UI.ViewModels;
using TagHierarchyManager.UI.Views;

namespace TagHierarchyManager.UI.Services;

public class DialogService
{
    public async Task<TResult?> ShowDialog<TResult>(Window dialog, object? dataContext = null)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return default;

        dialog.DataContext = dataContext;

        var ownerWindow = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
        if (ownerWindow == null)
            throw new InvalidOperationException("No owner window available to show dialog.");
        return await dialog.ShowDialog<TResult>(ownerWindow);
    }

    public Task ShowErrorDialog(string message) =>
        this.ShowDialog<object>(new ErrorDialog(), new ErrorDialogViewModel(message));
}