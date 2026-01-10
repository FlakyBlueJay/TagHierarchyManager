using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;
using TagHierarchyManager.UI.Views;

namespace TagHierarchyManager.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    internal TagDatabase Database;
    
    [ObservableProperty]
    private bool _unsavedChanges;
    
    private bool _isSwitching;
    
    [ObservableProperty]
    private HierarchyTreeViewModel _hierarchyTreeViewModel;
    
    // Since multiple view models will be using this tag, best to store it here as the authoritative source.
    private TagItemViewModel? _selectedTag;

    public TagItemViewModel? SelectedTag
    {
        get => _selectedTag;
        set
        {
            if (_selectedTag == value || _isSwitching) return;
            if (_selectedTag != null && this.UnsavedChanges)
            {
                _ = this.HandleTagSwitchAsync(this._selectedTag, value);
            }
            else
            {
                _selectedTag = value;
                HierarchyTreeViewModel.SelectedTag = value;
                _selectedTag?.BeginEdit();
                this.UnsavedChanges = false;
                this.OnPropertyChanged();
            }
        }
    }
    
    [ObservableProperty]
    private ObservableCollection<TagItemViewModel> _topLevelTags = [];

    [ObservableProperty]
    private bool _isDbLoaded;
    
    public int TotalTags => this.Database?.Tags.Count ?? 0;
    
    public string WindowTitle => IsDbLoaded
        ? string.Format(Resources.TitleWithDatabase, this.Database.Name)
        : Resources.Title;

    [ObservableProperty]
    private string _statusBlockText = Resources.StatusBlockReady;
    
    // TODO searchViewModel
    
    public MainWindowViewModel()
    {
    }

    public async Task<bool?> ShowNullableBoolDialog(Window dialog)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;
        
        var mainWindow = desktop.MainWindow;
        var result = await dialog.ShowDialog<bool?>(mainWindow);
        return result;
    }
    
    public async Task<bool?> ShowUnsavedChangesDialog()
    {
        var dialog = new UnsavedChangesDialog();

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        var mainWindow = desktop.MainWindow;
        var result = await dialog.ShowDialog<bool?>(mainWindow);
        return result;
    }

    public async Task StartTagDeletion()
    {
        var result = await this.ShowNullableBoolDialog(new DeleteTagDialog());
        if (result == null)
            return;

        if (result == true)
        {
            await DeleteSelectedTagAsync();
        }
    }

    private async Task DeleteSelectedTagAsync()
    {
        if (SelectedTag is null || this.Database is null) return;
        await this.Database.DeleteTag(SelectedTag.Tag.Id);
        _selectedTag = null;
        HierarchyTreeViewModel.SelectedTag = null;
        this.OnPropertyChanged(nameof(SelectedTag));
    }

    private async Task HandleTagSwitchAsync(TagItemViewModel? oldTag, TagItemViewModel? newTag)
    {
        _isSwitching = true;
        try
        {
            var result = await this.ShowNullableBoolDialog(new UnsavedChangesDialog());

            if (result == null)
            {
                _selectedTag = oldTag;
                HierarchyTreeViewModel.SelectedTag = oldTag;
                this.OnPropertyChanged(nameof(SelectedTag));
                return;
            }

            if (result == true)
                await this.SaveSelectedTagAsync();

            _selectedTag = newTag;
            HierarchyTreeViewModel.SelectedTag = newTag;
            _selectedTag?.BeginEdit();
            this.OnPropertyChanged(nameof(SelectedTag));
            this.UnsavedChanges = false;
        }
        finally
        {
            _isSwitching = false;
        }
        
    }
    
    public async Task LoadDatabase(string filePath)
    {
        this.IsDbLoaded = false;
        TagDatabase db = new();
        db.InitialisationComplete += this.TagDatabase_OnInitalisationComplete;
        await db.LoadAsync(filePath);
    }
    
    // some of this could end up in a TagEditorViewModel for the right pane, but it's not taking up too much space, so
    // it doesn't matter.
    public async Task SaveSelectedTagAsync()
    {
        if (SelectedTag is null || this.Database is null) return;
        
        SelectedTag.CommitEdit();
        await this.Database.WriteTagToDatabase(SelectedTag.Tag);
        this.StatusBlockText = $"Successfully saved tag {SelectedTag.Name}";
        this.UnsavedChanges = false;
    }
    
    private void TagDatabase_OnInitalisationComplete(object sender, EventArgs e)
    {
        if (sender is not TagDatabase db) return;
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            (this.HierarchyTreeViewModel as IDisposable)?.Dispose();
            
            this.Database = db;
            this.IsDbLoaded = true;
            this.HierarchyTreeViewModel = new HierarchyTreeViewModel(this);
            await this.HierarchyTreeViewModel.InitializeAsync();
            this.OnPropertyChanged(nameof(TotalTags));
            this.OnPropertyChanged(nameof(WindowTitle));
            this.Database.InitialisationComplete -= this.TagDatabase_OnInitalisationComplete;
            this.StatusBlockText = string.Format(Resources.StatusBlockDbLoadSuccessful, this.Database.Name);
        });
        Debug.WriteLine($"Database loaded on UI - name: {db.Name}, version: {db.Version}");
    }

    public void NewTag()
    {
        this.SelectedTag = new TagItemViewModel(
            new()
                { 
                    Name = string.Empty,
                    IsTopLevel = true
                }
            );
        this.SelectedTag.BeginEdit();
        this.UnsavedChanges = true;
    }
}