using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace TagHierarchyManager.UI.Views;

public partial class DeleteTagDialog : Window
{
    public DeleteTagDialog()
    {
        InitializeComponent();
    }

    public void ButtonYes_Click(object? sender, RoutedEventArgs e) => this.Close(true);
    public void ButtonNo_Click(object? sender, RoutedEventArgs e) => this.Close(false);
}