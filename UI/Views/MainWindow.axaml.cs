using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TagHierarchyManager.UI.ViewModels;

namespace TagHierarchyManager.UI.Views;

public partial class MainWindow : Window
{
    private bool _userWantsToQuit;

    public MainWindow()
    {
        this.InitializeComponent();
    }

    public MainWindowViewModel? ViewModel => this.DataContext as MainWindowViewModel;

    private static FilePickerFileType MusicBeeTagHierarchy { get; } = new("MusicBee tag hierarchy template")
    {
        Patterns = ["*.txt"]
    };

    // TODO resx
    private static FilePickerFileType TagDatabaseFileType { get; } = new("Tag Hierarchy Manager database")
    {
        Patterns = ["*.thdb"]
    };

    public void ButtonAdd_Click(object? sender, RoutedEventArgs e)
    {
        this.ViewModel.NewTag();
    }

    public void ButtonCancel_Click(object? sender, RoutedEventArgs e)
    {
        this.ViewModel.SelectedTag.BeginEdit();
    }

    public void ButtonDelete_Click(object? sender, RoutedEventArgs e)
    {
        this.ViewModel.StartTagDeletion();
    }

    public async void ButtonSave_Click(object? sender, RoutedEventArgs e)
    {
        await this.ViewModel.SaveSelectedTagAsync();
    }

    public async void MenuItemExport_Click(object? sender, RoutedEventArgs e)
    {
        var file = await this.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export tag database...",
                FileTypeChoices = [MusicBeeTagHierarchy]
            });
        if (file == null) return;
        var path = file.TryGetLocalPath();
        await this.ViewModel.ExportAsync(path);
    }

    public async void MenuItemNew_Click(object? sender, RoutedEventArgs e)
    {
        if (this.ViewModel.Database is null) return;
        var file = await this.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Create a new tag database...",
                FileTypeChoices = [MusicBeeTagHierarchy]
            });
        if (file == null) return;
        var path = file.TryGetLocalPath();
        await this.ViewModel.CreateNewDatabase(path);
    }

    public async void MenuItemOpen_Click(object? sender, RoutedEventArgs e)
    {
        var files = await this.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Open a tag database...",
                FileTypeFilter = [TagDatabaseFileType]
            });
        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath();
        if (path == null) return;
        await this.ViewModel.LoadDatabase(path);
    }

    public void MenuItemQuit_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    public void OpenAboutWindow(object? sender, RoutedEventArgs e)
    {
        new AboutWindow().ShowDialog(this);
    }

    public async void WindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (this._userWantsToQuit) return;
        if (this.ViewModel.UnsavedChanges)
        {
            e.Cancel = true;
            var result = await this.ViewModel.ShowNullableBoolDialog(new UnsavedChangesDialog());
            switch (result)
            {
                case true:
                    await this.ViewModel.SaveSelectedTagAsync();
                    this._userWantsToQuit = true;
                    this.Close();
                    break;
                case false:
                    this._userWantsToQuit = true;
                    this.Close();
                    break;
                case null:
                    break;
            }
        }
    }
}