using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TagHierarchyManager.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    public void OpenAboutWindow(object? sender, RoutedEventArgs e) => new AboutWindow().ShowDialog(this);
    public void Quit(object? sender, RoutedEventArgs e) => this.Close();
}