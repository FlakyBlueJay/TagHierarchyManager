using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
    // TODO searchViewModel
    // TODO hierarchyViewModel
    
    public MainWindowViewModel()
    {
    }
    
    // TagEditorViewModel, maybe?
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
    
    // TagEditorViewModel
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