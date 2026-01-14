using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TagHierarchyManager.UI.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        this.InitializeComponent();
    }

    public Uri ProjectUri { get; } = new(Assets.Resources.URL);

    public void OnOKClick(object? sender, RoutedEventArgs e) => this.Close();
}