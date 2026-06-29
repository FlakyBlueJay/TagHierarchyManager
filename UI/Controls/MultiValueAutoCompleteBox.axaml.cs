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
using TagHierarchyManager.Utilities;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Controls;

/// <summary>
///  The user control object for MultiValueAutoCompleteBox.
/// </summary>
public partial class MultiValueAutoCompleteBox : UserControl
{
    private const char Separator = ';';

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

    private SegmentData _currentSegment = new(string.Empty, 0, 0);

    /// <summary>
    /// Initialises a new instance of the <see cref="MultiValueAutoCompleteBox"/> class.
    /// </summary>
    public MultiValueAutoCompleteBox()
    {
        this.InitializeComponent();
        this.MultiValueAutoCompletePopup.PlacementTarget = this.MultiValueAutoCompleteBoxTextBox;
        this.MultiValueAutoCompleteListBox.ItemsSource = this.FilteredItems;
        this.MultiValueAutoCompleteBoxTextBox
            .GetPropertyChangedObservable(BoundsProperty)
            .Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(e =>
                this.MultiValueAutoCompleteListBox.Width = ((Rect)e.NewValue!).Width));
        this.MultiValueAutoCompleteBoxTextBox.AddHandler(KeyDownEvent, TextBox_OnKeyDown,
            RoutingStrategies.Bubble, handledEventsToo: true);
        this.MultiValueAutoCompleteListBox.AddHandler(PointerPressedEvent,
            ListBox_OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);

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
            (_, _) => this.RaiseEvent(new TextChangedEventArgs(TextChangedEvent));
    }

    /// <summary>
    /// Occurs when the text of the <see cref="MultiValueAutoCompleteBox"/> changes.
    /// </summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged
    {
        add => this.AddHandler(TextChangedEvent, value);
        remove => this.RemoveHandler(TextChangedEvent, value);
    }

    /// <summary>
    /// Gets or sets the source enumerable for the <see cref="MultiValueAutoCompleteBox"/>.
    /// </summary>
    public IEnumerable<TagItemViewModel>? ItemsSource
    {
        get => this.GetValue(ItemsSourceProperty);
        set => this.SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the text of the <see cref="MultiValueAutoCompleteBox"/>.
    /// </summary>
    public string Text
    {
        get => this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }
    
    /// <summary>
    /// Gets or sets the watermark of the <see cref="MultiValueAutoCompleteBox"/>.<br/>
    /// Avalonia 12 changed this to PlaceholderText, I'll have to do that some time as well.
    /// </summary>
    public string Watermark
    {
        get => this.GetValue(WatermarkProperty);
        set => this.SetValue(WatermarkProperty, value);
    }


    private bool CanShowPopup => (this._isTextBoxActive ||
                                  (this.MultiValueAutoCompleteListBox?.IsKeyboardFocusWithin ?? false))
                                 && this.FilteredItems.Any();

    private ObservableCollection<TagItemViewModel> FilteredItems { get; } = [];
    
    public void FocusTextBox()
    {
        this.MultiValueAutoCompleteBoxTextBox.Focus();
        this.MultiValueAutoCompleteBoxTextBox.CaretIndex = this.MultiValueAutoCompleteBoxTextBox.Text?.Length ?? 0;
    }
    
    public void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox box) return;
        var rawText = box.Text ?? string.Empty;
        if (box.Text == this._lastTypedRawText) return;
        this._lastTypedRawText = rawText;

        this.GetCurrentEditedSegment();
        Debug.WriteLine($"Current segment: {this._currentSegment.FullSegment}," +
                        $"Segment indexes: {this._currentSegment.IndexBack}-{this._currentSegment.IndexForward}," +
                        $"caret: {this.MultiValueAutoCompleteBoxTextBox.CaretIndex}");

        this.Text = rawText;
        this.RepopulateFilteredItems(this._currentSegment.FullSegment);
    }

    private void ApplyListBoxSelection(ListBox box, TagItemViewModel tag)
    {
        this._suppressPopup = true;
        box.SelectedItem = null;

        var result = this._lastTypedRawText[..this._currentSegment.IndexBack]
                     + (this._currentSegment.SpaceAtBeginning ? ' ' : string.Empty)
                     + tag.CurrentName
                     + this._lastTypedRawText[this._currentSegment.IndexForward..];
        this.Text = result;
        this.MultiValueAutoCompletePopup.IsOpen = false;
        this.MultiValueAutoCompleteBoxTextBox.Text = result;
        var finalCaretIndex = this._currentSegment.IndexBack + tag.CurrentName.Length;
        if (this._currentSegment.SpaceAtBeginning) finalCaretIndex++;
        this.MultiValueAutoCompleteBoxTextBox.CaretIndex = finalCaretIndex;
        this.MultiValueAutoCompleteBoxTextBox.Focus();
        this._lastTypedRawText = result;
        this._suppressPopup = false;
    }

    /// <summary>
    /// Finds the text segment being edited based on the current caret position.
    /// </summary>
    private void GetCurrentEditedSegment()
    {
        var caretIndex = this.MultiValueAutoCompleteBoxTextBox.CaretIndex;
        var backIndex = caretIndex > 0
            ? this._lastTypedRawText.LastIndexOf(Separator, caretIndex - 1)
            : -1;
        var forwardIndex = this._lastTypedRawText.IndexOf(Separator, caretIndex);

        if (backIndex == -1) backIndex = 0;
        else backIndex++;
        if (forwardIndex == -1) forwardIndex = this._lastTypedRawText.Length;

        var spaceAtBeginning = false;
        var activeSegment = this._lastTypedRawText[backIndex..forwardIndex];
        if (activeSegment.Length > 0 && activeSegment[0] == ' ') spaceAtBeginning = true;
        this._currentSegment = new SegmentData(activeSegment.Trim(), backIndex, forwardIndex, spaceAtBeginning);
    }

    /// <summary>
    /// Fires when the user clicks on a list item, running ApplyListBoxSelection.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ListBox_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (e.Source is not Control { DataContext: TagItemViewModel tag }) return;
        e.Handled = true;
        this.ApplyListBoxSelection(listBox, tag);
    }

    private void RepopulateFilteredItems(string itemName)
    {
        this.FilteredItems.Clear();
        var itemNameNormalised = StringNormaliser.FormatStringForSearch(itemName).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(itemName) && this.ItemsSource is not null)
        {
            var filtered = this.ItemsSource
                .DistinctBy(t => t.CurrentName)
                .Where(t =>
                        StringNormaliser.FormatStringForSearch(t.CurrentName)
                            .Contains(itemNameNormalised, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.CurrentName, StringComparer.OrdinalIgnoreCase).ToList();
            if (filtered.Count == 1 && filtered[0].CurrentName == this._currentSegment.FullSegment) return;
            foreach (var item in filtered)
                this.FilteredItems.Add(item);
        }

        this.UpdatePopupIsVisible();
    }

    private void TextBox_OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        this._isTextBoxActive = true;
    }

    private void TextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!this.MultiValueAutoCompletePopup.IsOpen) return;
        var listBox = this.MultiValueAutoCompleteListBox;
        switch (e.Key)
        {
            case Key.Down:
                listBox.SelectedIndex =
                    Math.Min(listBox.SelectedIndex + 1, this.FilteredItems.Count - 1);
                e.Handled = true;
                break;
            case Key.Up:
                listBox.SelectedIndex =
                    Math.Max(listBox.SelectedIndex - 1, 0);
                e.Handled = true;
                break;
            case Key.Enter when listBox.SelectedItem is TagItemViewModel tag:
                this.ApplyListBoxSelection(listBox, tag);
                e.Handled = true;
                break;
        }
    }

    private void TextBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        this._isTextBoxActive = false;
        this.UpdatePopupIsVisible();
    }

    /// <summary>
    /// Updates the popup visibility based on the current state of the control.
    /// </summary>
    private void UpdatePopupIsVisible()
    {
        if (this._suppressPopup) return;
        this.MultiValueAutoCompletePopup.IsOpen = this.CanShowPopup;
    }

    /// <summary>
    /// A record for storing the current segment of text being edited, for GetCurrentEditedSegment.
    /// </summary>
    /// <param name="FullSegment">The full string of the segment being edited.</param>
    /// <param name="IndexBack">The index of the control's Separator char when scanning backwards.</param>
    /// <param name="IndexForward">The index of the control's Separator char when scanning forwards.</param>
    /// <param name="SpaceAtBeginning">Denotes whether a space is at the beginning or not.</param>
    private record SegmentData(string FullSegment, int IndexBack, int IndexForward, bool SpaceAtBeginning = false);
}