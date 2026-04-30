using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TagHierarchyManager.UI.ViewModels;
using TagHierarchyManager.UI.Services;

namespace TagHierarchyManager.UI.Views;

public partial class MainWindow : Window
{
    private bool _userWantsToQuit;
    private readonly DialogService _dialogService;

    public MainWindow() : this(new DialogService()) { }
    
    public MainWindow(DialogService dialogService)
    {
        this.InitializeComponent();
        this._dialogService = dialogService;
    }


    private MainWindowViewModel? ViewModel => this.DataContext as MainWindowViewModel;

    public void MenuItemAbout_Click(object? sender, RoutedEventArgs e)
    {
        new AboutWindow().ShowDialog(this);
    }

    public void MenuItemQuit_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    public async void WindowClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            if (this._userWantsToQuit) return;
            e.Cancel = true;
            if (this.ViewModel is not null && !await this.ViewModel.ConfirmQuitAsync()) return;
        
            this._userWantsToQuit = true;
            this.Close();
        }
        catch (Exception ex)
        {
            await this._dialogService.ShowErrorDialog(ex.Message);
        }
    }
}