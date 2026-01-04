using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TagHierarchyManager.UI.Views;

public partial class AboutWindow : Window
{
    public Uri ProjectUri { get; } = new(Assets.Resources.URL);
    
    public AboutWindow()
    {
        InitializeComponent();
    }
    
    public void OnOKClick(object? sender, RoutedEventArgs e) => this.Close();
}