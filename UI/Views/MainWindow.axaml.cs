using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TagHierarchyManager.Common;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class MainWindow : Window
{
    private bool _userWantsToQuit;

    public MainWindow()
    {
        this.InitializeComponent();
    }

    private MainWindowViewModel? ViewModel => this.DataContext as MainWindowViewModel;

    public void ButtonAdd_Click(object? sender, RoutedEventArgs e)
    {
        this.ViewModel?.NewTag();
    }

    public void ButtonCancel_Click(object? sender, RoutedEventArgs e)
    {
        this.ViewModel?.SelectedTag?.BeginEdit();
    }

    public void ButtonDelete_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (this.ViewModel?.Database is null) return;
            this.ViewModel?.StartTagDeletion();
        }
        catch (Exception ex)
        {
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }
    
    public async void ButtonSave_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (this.ViewModel?.SelectedTag is null) return;
            await this.ViewModel.SaveSelectedTagAsync();
        }
        catch (Exception ex)
        {
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }

    public void ButtonSearch_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (this.ViewModel?.Database is null) return;
            this.ViewModel?.StartSearch(
                this.SearchTextBox.Text!,
                (TagDatabaseSearchMode)this.SearchModeComboBox.SelectedIndex,
                this.SearchAliasesCheckBox.IsChecked ?? false);
        }
        catch (Exception ex)
        {
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
        
    }

    public void MenuItemDatabaseSettings_Click(object? sender, RoutedEventArgs e)
    {
        if (this.ViewModel?.Database == null) return;
        this.ViewModel.ShowDatabaseSettings();
    }

    // Resharper disable once AsyncVoidEventHandlerMethod
    public async void MenuItemExport_Click(object? sender, RoutedEventArgs e)
    {
        if (this.ViewModel?.Database == null) return;
        try
        {
            var file = await this.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = Assets.Resources.DialogTitleExportTagDatabase,
                    FileTypeChoices = [Common.MusicBeeTagHierarchy],
                    SuggestedFileName = this.ViewModel.Database.Name
                });
            var path = file?.TryGetLocalPath();
            if (path == null) return;
            await this.ViewModel.ExportAsync(path);
        }
        catch (Exception ex)
        {
            this.ViewModel.IsDbEnabled = true;
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }

    public void MenuItemImport_Click(object? sender, RoutedEventArgs e)
    {
        this.ViewModel?.ShowImportDialog();
    }

    public async void MenuItemNew_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (this.ViewModel is null) return;
            var file = await this.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = Assets.Resources.DialogTitleSaveDatabaseAs,
                    FileTypeChoices = [Common.TagDatabaseFileType]
                });
            var path = file?.TryGetLocalPath();
            if (path == null) return;
            await this.ViewModel.CreateNewDatabase(path);
        }
        catch (Exception ex)
        {
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }

    public async void MenuItemOpen_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (this.ViewModel is null) return;
            var files = await this.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = Assets.Resources.DialogTitleOpenDatabase,
                    FileTypeFilter = [Common.TagDatabaseFileType]
                });
            if (files.Count == 0) return;
            var path = files[0].TryGetLocalPath();
            if (path == null) return;
            await this.ViewModel.LoadDatabase(path);
        }
        catch (Exception ex)
        {
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }


    public void MenuItemQuit_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    public void OpenAboutWindow(object? sender, RoutedEventArgs e)
    {
        new AboutWindow().ShowDialog(this);
    }

    public void SearchTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        this.ButtonSearch_Click(sender, e);
        e.Handled = true;
    }

    public async void WindowClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            if (this._userWantsToQuit) return;
            if (this.ViewModel is not null && this.ViewModel.UnsavedChanges)
            {
                e.Cancel = true;
                var result = await this.ViewModel.ShowNullableBoolDialog(new UnsavedChangesDialog());
                switch (result)
                {
                    case true:
                        await this.ViewModel.SaveSelectedTagAsync();
                        this._userWantsToQuit = true;
                        this.Close();
                        break;
                    case false:
                        this._userWantsToQuit = true;
                        this.Close();
                        break;
                    case null:
                        break;
                }
            }
            else
            {
                this._userWantsToQuit = true;
                this.Close();
            }
        }
        catch (Exception ex)
        {
            var error = new ErrorDialogViewModel(ex.Message);
            error.ShowDialog();
        }
    }
}