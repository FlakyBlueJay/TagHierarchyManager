using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.UI.ViewModels;

public class TagItemViewModel(Tag tag, Func<int, string?> getNameById) : ViewModelBase
{
    internal Tag Tag { get; } = tag;

    public int Id => Tag.Id;
    
    public string Name => Tag.Name;

    private string _editingName = tag.Name;
    public string EditingName
    {
        get => _editingName;
        set
        {
            if (_editingName == value) return;
            _editingName = value;
            OnPropertyChanged();
        }
    }

    private string Parents => Tag.ParentIds.Count > 0 
        ? string.Join("; ", Tag.ParentIds.Select(getNameById).Where(n => n != null)) 
        : string.Empty;
    
    private string _editingParents;
    public string EditingParents
    {
        get => _editingParents;
        set
        {
            if (_editingParents == value) return;
            _editingParents = value;
            OnPropertyChanged();
        }
    }
    
    public string TagBindings => Tag.TagBindings.Count > 0 
        ? string.Join("; ", Tag.TagBindings) 
        : string.Empty;
    private string _editingTagBindings;

    public string EditingTagBindings
    {
        get => _editingTagBindings;
        set
        {
            if (_editingTagBindings == value) return;
            _editingTagBindings = value;
            OnPropertyChanged();
        }
    }
    
    public string Aliases => Tag.Aliases.Count > 0 
        ? string.Join("; ", Tag.Aliases) 
        : string.Empty;
    private string _editingAliases;

    public string EditingAliases
    {
        get => _editingAliases;
        set
        {
            if (_editingAliases == value) return;
            _editingAliases = value;
            OnPropertyChanged();
        }
    }

    public string Notes => Tag.Notes;
    private string _editingNotes;

    public string EditingNotes
    {
        get => _editingNotes;
        set
        {
            if (_editingNotes == value) return;
            _editingNotes = value;
            OnPropertyChanged();
        }
    }
    
    private bool IsTopLevel => Tag.IsTopLevel;
    private bool _editingIsTopLevel;
    public bool EditingIsTopLevel
    {
        get => _editingIsTopLevel;
        set
        {
            if (_editingIsTopLevel == value) return;
            _editingIsTopLevel = value;
            OnPropertyChanged();
        }
    }
    
    
    public ObservableCollection<TagItemViewModel> Children { get; } = [];

    public void BeginEdit()
    {
        EditingName = Tag.Name;
        this._editingParents = Parents;
        this._editingIsTopLevel = IsTopLevel;
        this._editingTagBindings = TagBindings;
        this._editingAliases = Aliases;
        this._editingNotes = Notes;
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
        Tag.Notes = EditingNotes;
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
        OnPropertyChanged(nameof(Parents));
        _editingParents = Parents; 
        OnPropertyChanged(nameof(EditingParents));
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