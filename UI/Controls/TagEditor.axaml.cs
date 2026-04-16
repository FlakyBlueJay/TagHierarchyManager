using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views.Controls;

public partial class TagEditor : UserControl
{
    private bool _suppressEditEvents;

    public TagEditor()
    {
        this.InitializeComponent();
    }

    private TagEditorViewModel? ViewModel => this.DataContext as TagEditorViewModel;

    protected override void OnDataContextChanged(EventArgs e)
    {
        this._suppressEditEvents = true;
        base.OnDataContextChanged(e);
        Dispatcher.UIThread.Post(() => this._suppressEditEvents = false);
    }

    private void OnTagCheckboxChanged(object? sender, RoutedEventArgs e)
    {
        if (this._suppressEditEvents) return;
        if (sender is not CheckBox { IsFocused: true }) return;
        if (this.ViewModel?.FlagUnsavedChangesCommand.CanExecute(null) != true) return;
        this.ViewModel.FlagUnsavedChangesCommand.Execute(null);
    }

    private void OnTagTextboxEdited(object? sender, TextChangedEventArgs e)
    {
        if (this._suppressEditEvents) return;
        if (sender is not TextBox { IsFocused: true }) return;
        if (this.ViewModel?.FlagUnsavedChangesCommand.CanExecute(null) != true) return;
        this.ViewModel.FlagUnsavedChangesCommand.Execute(null);
    }
}