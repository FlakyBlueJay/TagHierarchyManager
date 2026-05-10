using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.ViewModels;
using TagHierarchyManager.UI.Views;

namespace TagHierarchyManager.UI.Services;

public class DialogService
{
    public async Task<TResult?> ShowDialog<TResult>(Window dialog, object? dataContext = null)
    {
        var ownerWindow = this.GetOwnerWindow();
        dialog.DataContext = dataContext;
        return await dialog.ShowDialog<TResult>(ownerWindow);
    }

    public Task ShowErrorDialog(string message)
    {
        return this.ShowDialog<object>(new ErrorDialog(), new ErrorDialogViewModel(message));
    }

    public Task ShowAmbiguousSaveDialog(Tag tag, List<Tag> parentTags)
    {
        var viewModel = new SaveAmbiguousViewModel(tag, parentTags);   
        return this.ShowDialog<object>(new SaveAmbiguousDialog(), viewModel);
    }

    private Window GetOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new InvalidOperationException("Not in a desktop environment.");
        
        var ownerWindow = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
        return ownerWindow ?? throw new InvalidOperationException("No owner window available to show dialog.");
    }
}