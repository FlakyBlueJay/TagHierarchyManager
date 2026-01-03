using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace TagHierarchyManager.UI;

public partial class AboutWindow : Window
{
    public Uri ProjectUri { get; } = new(Assets.Resources.URL);
    
    public AboutWindow()
    {
        InitializeComponent();
    }
    
    public void OnOKClick(object? sender, RoutedEventArgs e) => this.Close();
}