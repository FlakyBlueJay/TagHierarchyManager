using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    internal TagDatabase Database;

    [ObservableProperty]
    private HierarchyTreeViewModel _hierarchyTreeViewModel;
    
    // Since multiple view models will be using this tag, best to store it here as the authoritative source.
    [ObservableProperty]
    private TagItemViewModel? _selectedTag;
    
    [ObservableProperty]
    private ObservableCollection<TagItemViewModel> _topLevelTags = [];

    [ObservableProperty]
    private bool _isDbLoaded;
    
    public int TotalTags => this.Database?.Tags.Count ?? 0;
    
    public string WindowTitle => IsDbLoaded
        ? string.Format(Resources.TitleWithDatabase, this.Database.Name)
        : Resources.Title;

    public string StatusBlockText = Resources.StatusBlockReady;
    
    // TODO searchViewModel
    
    public MainWindowViewModel()
    {
    }
    
    partial void OnSelectedTagChanged(TagItemViewModel? value)
    {
        value?.BeginEdit();
    }

    public async Task LoadDatabase(string filePath)
    {
        this.IsDbLoaded = false;
        TagDatabase db = new();
        db.InitialisationComplete += OnDatabaseLoaded;
        await db.LoadAsync(filePath);
    }
    
    // some of this could end up in a TagEditorViewModel for the right pane, but it's not taking up too much space, so
    // it doesn't matter.
    public async Task SaveTag()
    {
        if (SelectedTag is null || this.Database is null) return;
        var oldName = SelectedTag.Name;
        
        SelectedTag.CommitEdit();
        await this.Database.WriteTagToDatabase(SelectedTag.Tag);
    }
    
    private void OnDatabaseLoaded(object sender, EventArgs e)
    {
        if (sender is not TagDatabase db) return;
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            this.Database = db;
            this.IsDbLoaded = true;
            this.HierarchyTreeViewModel = new HierarchyTreeViewModel(this);
            await this.HierarchyTreeViewModel.InitializeAsync();
            this.OnPropertyChanged(nameof(TotalTags));
            this.OnPropertyChanged(nameof(WindowTitle));
            this.Database.InitialisationComplete -= OnDatabaseLoaded;
        });
        Debug.WriteLine($"Database loaded on UI - name: {db.Name}, version: {db.Version}");
    }
}