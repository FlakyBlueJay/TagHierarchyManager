using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TagHierarchyManager.Common;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

/*
 * TODO:
 * add "recently added" functionality.
 */
public partial class MainWindow : Window
{
    private bool _userWantsToQuit;

    public MainWindow()
    {
        this.InitializeComponent();
    }

    private MainWindowViewModel? ViewModel => this.DataContext as MainWindowViewModel;

    // TODO make relay command
    public void ButtonAdd_Click(object? sender, RoutedEventArgs e)
    {
        this.ViewModel?.NewTag();
    }

    // TODO make relay command
    public void ButtonBulkAdd_Click(object? sender, RoutedEventArgs e)
    {
        this.ViewModel?.ShowBulkAddDialog();
    }

    // TODO make relay command
    public void ButtonSearch_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (this.ViewModel is null) return;
            this.ViewModel.StartSearch(
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

    // TODO make relay command?
    public void MenuItemDatabaseSettings_Click(object? sender, RoutedEventArgs e)
    {
        this.ViewModel?.ShowDatabaseSettings();
    }

    // TODO make relay command?
    public void MenuItemImport_Click(object? sender, RoutedEventArgs e)
    {
        this.ViewModel?.ShowImportDialog();
    }


    public void MenuItemQuit_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    // make relay command?
    public void OpenAboutWindow(object? sender, RoutedEventArgs e)
    {
        new AboutWindow().ShowDialog(this);
    }

    // make relay command?
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
                var result = await this.ViewModel.ShowUnsavedChangesDialog();
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