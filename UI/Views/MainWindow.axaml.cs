using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

    public void MenuItemAbout_Click(object? sender, RoutedEventArgs e)
    {
        new AboutWindow().ShowDialog(this);
    }

    public void MenuItemQuit_Click(object? sender, RoutedEventArgs e) =>
        this.Close();

    public void SearchTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        var args = new object[]
        {
            this.SearchTextBox.Text ?? "",
            this.SearchModeComboBox.SelectedIndex,
            this.SearchAliasesCheckBox.IsChecked ?? false
        };

        if (this.ViewModel?.StartSearchCommand.CanExecute(args) != true) return;
        this.ViewModel.StartSearchCommand.Execute(args);
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
                        if (this.ViewModel?.SaveSelectedTagCommand.CanExecute(null) != true) return;
                        this.ViewModel.SaveSelectedTagCommand.Execute(null);
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