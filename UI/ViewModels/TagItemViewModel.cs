using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.UI.ViewModels;

public partial class TagItemViewModel(Tag tag, Func<int, string?> getNameById) : ViewModelBase
{
    internal Tag Tag { get; } = tag;

    public int Id => Tag.Id;
    
    public string Name => Tag.Name;

    [ObservableProperty]
    private string _editingName = tag.Name;

    private string Parents => Tag.ParentIds.Count > 0 
        ? string.Join("; ", Tag.ParentIds.Select(getNameById).Where(n => n != null)) 
        : string.Empty;
    
    [ObservableProperty]
    private string _editingParents;
    
    public string TagBindings => Tag.TagBindings.Count > 0 
        ? string.Join("; ", Tag.TagBindings) 
        : string.Empty;
    
    [ObservableProperty]
    private string _editingTagBindings;
    
    public string Aliases => Tag.Aliases.Count > 0 
        ? string.Join("; ", Tag.Aliases) 
        : string.Empty;
    
    [ObservableProperty]
    private string _editingAliases;
    
    public string Notes => Tag.Notes;
    
    [ObservableProperty]
    private string _editingNotes;
    
    private bool IsTopLevel => Tag.IsTopLevel;
    [ObservableProperty]
    private bool _editingIsTopLevel;
    
    
    public ObservableCollection<TagItemViewModel> Children { get; } = [];

    private bool _isInitialising;
    
    public event EventHandler? UserEditedTag;
    
    public void BeginEdit()
    {
        _isInitialising = true;
        EditingName = Tag.Name;
        this.EditingParents = Parents;
        this.EditingIsTopLevel = IsTopLevel;
        this.EditingTagBindings = TagBindings;
        this.EditingAliases = Aliases;
        this.EditingNotes = Notes;
        _isInitialising = false;
    }
    
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (!_isInitialising && e.PropertyName.StartsWith("Editing"))
        {
            UserEditedTag.Invoke(this, EventArgs.Empty);
        }
    }

    public void CommitEdit()
    {
        Tag.Name = EditingName;
        Tag.Parents = !string.IsNullOrWhiteSpace(EditingParents)
            ? EditingParents.Split(';', StringSplitOptions.RemoveEmptyEntries | 
                StringSplitOptions.TrimEntries)
                .ToList()
            : [];
        Tag.TagBindings = !string.IsNullOrWhiteSpace(EditingTagBindings)
            ? EditingTagBindings.Split(';', StringSplitOptions.RemoveEmptyEntries | 
                                        StringSplitOptions.TrimEntries)
                .ToList()
            : [];
        Tag.Aliases = !string.IsNullOrWhiteSpace(EditingAliases)
            ? EditingAliases.Split(';', StringSplitOptions.RemoveEmptyEntries | 
                                            StringSplitOptions.TrimEntries)
                .ToList()
            : [];
        Tag.Notes = !string.IsNullOrWhiteSpace(EditingNotes) ? EditingNotes : "";
        Tag.IsTopLevel = EditingIsTopLevel;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Parents));
        OnPropertyChanged(nameof(Aliases));
        OnPropertyChanged(nameof(TagBindings));
        OnPropertyChanged(nameof(Notes));
        OnPropertyChanged(nameof(IsTopLevel));
        RefreshParentsString();
    }

    public void RefreshParentsString()
    {
        _isInitialising = true;
        OnPropertyChanged(nameof(Parents));
        _editingParents = Parents; 
        OnPropertyChanged(nameof(EditingParents));
        _isInitialising = false;
    }
    
    
    public void SyncChildren(List<TagItemViewModel> children)
    {
        var newChildren = children.Select(c => c.Id).ToHashSet();
        
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (!newChildren.Contains(Children[i].Id))
            {
                Children.RemoveAt(i);
            }
        }

        var currentIds = Children.Select(c => c.Id).ToHashSet();
        foreach (var child in children)
        {
            if (!currentIds.Contains(child.Id))
            {
                // Find the correct index to maintain alphabetical order
                int index = 0;
                while (index < Children.Count && string.Compare(Children[index].Name, child.Name, StringComparison.CurrentCultureIgnoreCase) < 0)
                {
                    index++;
                }
                Children.Insert(index, child);
            }
        }
            
    }
}