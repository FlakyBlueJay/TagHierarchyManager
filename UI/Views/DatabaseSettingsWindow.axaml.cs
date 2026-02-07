using Avalonia.Controls;
using Avalonia.Interactivity;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class DatabaseSettingsWindow : Window
{
    public DatabaseSettingsWindow()
    {
        this.InitializeComponent();
        
        this.DataContextChanged += (_, _) =>
        {
            if (this.ViewModel != null) this.ViewModel.RequestClose += this.Close;
        };

        this.Unloaded += (_, _) =>
        {
            if (this.ViewModel != null)
                this.ViewModel.RequestClose -= this.Close;
        };
    }

    private DatabaseSettingsViewModel? ViewModel => this.DataContext as DatabaseSettingsViewModel;
    
    public void ButtonCancel_Click(object? sender, RoutedEventArgs e) => this.Close();
}