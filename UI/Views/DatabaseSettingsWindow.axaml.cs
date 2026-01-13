using Avalonia.Controls;
using Avalonia.Interactivity;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class DatabaseSettingsWindow : Window
{
    public DatabaseSettingsWindow()
    {
        this.InitializeComponent();
    }
    
    private DatabaseSettingsViewModel? ViewModel => this.DataContext as DatabaseSettingsViewModel;
    
    public void ButtonCancel_Click(object? sender, RoutedEventArgs e) => this.Close();
    
    public void ButtonOk_Click(object? sender, RoutedEventArgs e)
    {
        this.ViewModel!.SaveSettings();
        this.Close();
    }
}