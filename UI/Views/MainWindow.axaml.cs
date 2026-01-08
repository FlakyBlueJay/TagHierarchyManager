using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class MainWindow : Window
{
    public MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;
    private bool _userWantsToQuit;
    
    public MainWindow()
    {
        InitializeComponent();
    }
    
    // TODO resx
    private static FilePickerFileType TagDatabaseFileType { get; } = new("Tag Hierarchy Manager database")
    {
        Patterns = ["*.thdb"]
    };
    
    public void OpenAboutWindow(object? sender, RoutedEventArgs e) => new AboutWindow().ShowDialog(this);
    public void MenuItemQuit_Click(object? sender, RoutedEventArgs e) => this.Close();
    
    public async void MenuItemOpen_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await this.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = "Open a tag database...",
                    FileTypeFilter = [TagDatabaseFileType]
                });
            if (files.Count == 0) return;
            var path = files[0].TryGetLocalPath();
            if (path == null) return;
            await this.ViewModel.LoadDatabase(path);
        }
        catch (Exception ex)
        {
            throw; // TODO handle exception
        }
    }

    public async void ButtonSave_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            await this.ViewModel.SaveSelectedTagAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    
    public void ButtonCancel_Click(object? sender, RoutedEventArgs e)
    {
        this.ViewModel.SelectedTag.BeginEdit();
    }

    public async void WindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (this._userWantsToQuit) return;
        if (this.ViewModel.UnsavedChanges)
        {
            e.Cancel = true;
            var result = await this.ViewModel.ShowUnsavedChangesDialog();
            switch (result)
            {
                case UnsavedChangesResult.Save:
                    await this.ViewModel.SaveSelectedTagAsync();
                    _userWantsToQuit = true;
                    this.Close();
                    break;
                case UnsavedChangesResult.Discard:
                    _userWantsToQuit = true;
                    this.Close();
                    break;
                case UnsavedChangesResult.Cancel:
                default:
                    break;
            }
        }
    }
}