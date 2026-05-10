using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    public static readonly StyledProperty<IEnumerable<TagItemViewModel>?> ItemsSourceProperty =
        AvaloniaProperty.Register<MultiValueAutoCompleteBox, IEnumerable<TagItemViewModel>?>(
            nameof(ItemsSource), []);

    // ReSharper disable once MemberCanBePrivate.Global
    // this is just wrong, setting this private will cause the app to crash on boot.
    public static readonly RoutedEvent<TextChangedEventArgs> TextChangedEvent =
        RoutedEvent.Register<MultiValueAutoCompleteBox, TextChangedEventArgs>(nameof(TextChanged),
            RoutingStrategies.Bubble);

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MultiValueAutoCompleteBox, string>(
            nameof(Text), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<MultiValueAutoCompleteBox, string>(
            nameof(Watermark), string.Empty);

    private bool _isTextBoxActive;

    private string _lastTypedRawText = string.Empty;

    private bool _suppressPopup;

    public MultiValueAutoCompleteBox()
    {
        this.InitializeComponent();
        this.MultiValueAutoCompletePopup.PlacementTarget = this.MultiValueAutoCompleteBoxTextBox;
        this.MultiValueAutoCompleteListBox.ItemsSource = this.FilteredItems;
        this.MultiValueAutoCompleteBoxTextBox
            .GetPropertyChangedObservable(BoundsProperty)
            .Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(e =>
                this.MultiValueAutoCompleteListBox.Width = ((Rect)e.NewValue!).Width));
        this.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            if (this.MultiValueAutoCompleteBoxTextBox.Text is null) return;
            this._lastTypedRawText = this.MultiValueAutoCompleteBoxTextBox.Text;
            this.FilteredItems.Clear();
            this.MultiValueAutoCompleteBoxTextBox.Focus();
            e.Handled = true;
        };
        this.MultiValueAutoCompleteBoxTextBox.TextChanged +=
            (s, e) => this.RaiseEvent(new TextChangedEventArgs(TextChangedEvent));
    }

    public event EventHandler<TextChangedEventArgs>? TextChanged
    {
        add => this.AddHandler(TextChangedEvent, value);
        remove => this.RemoveHandler(TextChangedEvent, value);
    }

    public IEnumerable<TagItemViewModel>? ItemsSource
    {
        get => this.GetValue(ItemsSourceProperty);
        set => this.SetValue(ItemsSourceProperty, value);
    }

    public string Text
    {
        get => this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    public string Watermark
    {
        get => this.GetValue(WatermarkProperty);
        set => this.SetValue(WatermarkProperty, value);
    }

    private bool CanShowPopup => (this._isTextBoxActive ||
                                  (this.MultiValueAutoCompleteListBox?.IsKeyboardFocusWithin ?? false))
                                 && this.FilteredItems.Any();

    private ObservableCollection<TagItemViewModel> FilteredItems { get; } = [];

    public void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox box) return;
        var rawText = box.Text ?? string.Empty;
        if (box.Text == this._lastTypedRawText) return;
        this._lastTypedRawText = rawText;
        this.Text = rawText;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        Debug.WriteLine(
            $"CanShowPopup: {this.CanShowPopup}, focused: {this._isTextBoxActive}, items: {this.FilteredItems.Count}");
        // ReSharper disable once InvertIf
        if (change.Property == TextProperty)
        {
            var last = this.Text.Split(';').Last();
            last = last.Trim();
            this.RepopulateFilteredItems(last);
        }
    }

    private void ApplyListBoxSelection(ListBox box, TagItemViewModel tag)
    {
        this._suppressPopup = true;
        box.SelectedItem = null;

        var lastSemicolon = this._lastTypedRawText.LastIndexOf(';');
        var prefix = lastSemicolon >= 0 ? this._lastTypedRawText[..lastSemicolon].TrimEnd() + "; " : string.Empty;
        var result = $"{prefix}{tag.CurrentName}";
        this.Text = result;
        this.MultiValueAutoCompletePopup.IsOpen = false;
        this.MultiValueAutoCompleteBoxTextBox.Text = result;
        this.MultiValueAutoCompleteBoxTextBox.CaretIndex = result.Length;
        this.MultiValueAutoCompleteBoxTextBox.Focus();
        this._lastTypedRawText = result;
        this._suppressPopup = false;
    }

    private void ListBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not ListBox { SelectedItem: TagItemViewModel tag } listBox) return;

        if (e.Key == Key.Enter)
            this.ApplyListBoxSelection(listBox, tag);
    }

    private void ListBox_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not ListBox { SelectedItem: TagItemViewModel tag } listBox) return;
        this.ApplyListBoxSelection(listBox, tag);
    }

    private void RepopulateFilteredItems(string itemName)
    {
        this.FilteredItems.Clear();

        if (!string.IsNullOrWhiteSpace(itemName) && this.ItemsSource is not null)
        {
            var filtered = this.ItemsSource
                .Where(t => t.CurrentName.Contains(itemName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.CurrentName, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var item in filtered)
                this.FilteredItems.Add(item);
        }

        this.UpdatePopupIsVisible();
    }

    private void TextBox_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        this._isTextBoxActive = true;
    }

    private void TextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Down) return;
        if (!this.MultiValueAutoCompletePopup.IsOpen) return;
        this.MultiValueAutoCompleteListBox.SelectedIndex = 0;
        var first = this.MultiValueAutoCompleteListBox.ContainerFromIndex(0);
        first?.Focus();
        e.Handled = true;
    }

    private void TextBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        this._isTextBoxActive = false;
        this.UpdatePopupIsVisible();
    }

    private void UpdatePopupIsVisible()
    {
        if (this._suppressPopup) return;
        this.MultiValueAutoCompletePopup.IsOpen = this.CanShowPopup;
    }
}