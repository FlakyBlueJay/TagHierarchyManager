using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Controls;

public partial class MultiValueAutoCompleteBox : UserControl
{
    public MultiValueAutoCompleteBox()
    {
        InitializeComponent();
        this.MultiValueAutoCompletePopup.PlacementTarget = this.MultiValueAutoCompleteBoxTextBox;
        this.MultiValueAutoCompleteListBox.ItemsSource = this.FilteredItems;
        this.MultiValueAutoCompleteBoxTextBox
            .GetPropertyChangedObservable(BoundsProperty)
            .Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                e => this.MultiValueAutoCompleteListBox.Width = ((Rect)e.NewValue!).Width));
    }
    
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MultiValueAutoCompleteBox, string>(
            nameof(Text), defaultValue: string.Empty, defaultBindingMode: BindingMode.TwoWay);
    
    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<MultiValueAutoCompleteBox, string>(
            nameof(Watermark), defaultValue: string.Empty);
    
    public static readonly StyledProperty<IEnumerable<TagItemViewModel>?> ItemsSourceProperty =
        AvaloniaProperty.Register<MultiValueAutoCompleteBox, IEnumerable<TagItemViewModel>?>(
            nameof(ItemsSource), defaultValue: []);

    private bool _isTextBoxFocused;
    
    private bool CanShowPopup => this._isTextBoxFocused && this.FilteredItems.Any();
    
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }
    
    public IEnumerable<TagItemViewModel>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    
    public ObservableCollection<TagItemViewModel> FilteredItems { get; set; } = [];

    private string _lastTypedRawText = string.Empty;
    
    public void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox box) return;
        var rawText = box.Text ?? string.Empty;
        if (box.Text == this._lastTypedRawText) return;
        this._lastTypedRawText = rawText;
        this.Text = rawText;
    }
    
    private void TextBox_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        this._isTextBoxFocused = true;
    }
    
    private void TextBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        this._isTextBoxFocused = false;
        this.UpdatePopupIsVisible();
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        System.Diagnostics.Debug.WriteLine($"CanShowPopup: {this.CanShowPopup}, focused: {this._isTextBoxFocused}, items: {this.FilteredItems.Count}");
        if (change.Property == TextProperty)
        {
            var last = this.Text.Split(';').Last();
            last = last.Trim();
            this.RepopulateFilteredItems(last);
        }
    }
    
    private void RepopulateFilteredItems(string itemName)
    {
        this.FilteredItems.Clear();

        if (!string.IsNullOrWhiteSpace(itemName) && this.ItemsSource is not null)
        {
            var filtered = this.ItemsSource
                .Where(t => t.CurrentName.Contains(itemName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var item in filtered)
                this.FilteredItems.Add(item);
            
        }
        
        this.UpdatePopupIsVisible();
    }

    private bool _isApplyingSelection;
    private void UpdatePopupIsVisible()
    {
        if (this._isApplyingSelection) return;
        this.MultiValueAutoCompletePopup.IsOpen = this.CanShowPopup;
    }
    
    private void ListBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox || listBox.SelectedItem is not TagItemViewModel tag) return;
        this._isApplyingSelection = true;
        listBox.SelectedItem = null;
        
        var lastSemicolon = this._lastTypedRawText.LastIndexOf(';');
        var prefix = lastSemicolon >= 0 ? this._lastTypedRawText[..(lastSemicolon)].TrimEnd() + "; " : string.Empty;
        var result = $"{prefix}{tag.CurrentName}";
        this.Text = result;
        this.MultiValueAutoCompletePopup.IsOpen = false;
        this.MultiValueAutoCompleteBoxTextBox.Text = result;
        this.MultiValueAutoCompleteBoxTextBox.CaretIndex = result.Length;
        this.MultiValueAutoCompleteBoxTextBox.Focus();
        this._lastTypedRawText = result;
        this._isApplyingSelection = false;
    }
}