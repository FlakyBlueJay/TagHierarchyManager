using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class TagItemViewModel(Tag tag, Func<int, string?>? getNameById = null) : ViewModelBase
{
    [ObservableProperty] private string _editingAliases = string.Empty;

    [ObservableProperty] private bool _editingIsTopLevel;

    [ObservableProperty] private string _editingName = tag.Name;

    [ObservableProperty] private string _editingNotes = string.Empty;

    [ObservableProperty] private string _editingParents = string.Empty;

    [ObservableProperty] private string _editingTagBindings = string.Empty;

    private bool _isInitialising;

    public event EventHandler? UserEditedTag;

    public string Aliases =>
        this.Tag.Aliases.Count > 0
            ? string.Join("; ", this.Tag.Aliases)
            : string.Empty;


    public ObservableCollection<TagItemViewModel> Children { get; } = [];

    public int Id => this.Tag.Id;

    public string Name => this.Tag.Name;

    public string Notes => this.Tag.Notes;

    public bool OnDatabase => this.Id != 0;
    
    public bool HasChildren => this.Children.Count > 0;

    public string TagBindings =>
        this.Tag.TagBindings.Count > 0
            ? string.Join("; ", this.Tag.TagBindings)
            : string.Empty;

    internal Tag Tag { get; } = tag;

    private bool IsTopLevel => this.Tag.IsTopLevel;

    private string Parents => getNameById != null && this.Tag.ParentIds is { Count: > 0 } 
        ? string.Join("; ", this.Tag.ParentIds.Select(getNameById).Where(n => n != null))
        : string.Empty;

    public void BeginEdit()
    {
        this._isInitialising = true;
        this.EditingName = this.Tag.Name;
        
        if (this.OnDatabase || string.IsNullOrEmpty(this.EditingParents))
            this.EditingParents = this.Parents;
        
        this.EditingIsTopLevel = this.IsTopLevel;
        this.EditingTagBindings = this.TagBindings;
        this.EditingAliases = this.Aliases;
        this.EditingNotes = this.Notes;
        this._isInitialising = false;
    }

    public void CommitEdit()
    {
        this.Validate();
        this.Tag.Name = this.EditingName;
        this.Tag.Parents = !string.IsNullOrWhiteSpace(this.EditingParents)
            ? this.EditingParents.Split(';', StringSplitOptions.RemoveEmptyEntries |
                                             StringSplitOptions.TrimEntries)
                .ToList()
            : [];
        this.Tag.TagBindings = !string.IsNullOrWhiteSpace(this.EditingTagBindings)
            ? this.EditingTagBindings.Split(';', StringSplitOptions.RemoveEmptyEntries |
                                                 StringSplitOptions.TrimEntries)
                .ToList()
            : [];
        this.Tag.Aliases = !string.IsNullOrWhiteSpace(this.EditingAliases)
            ? this.EditingAliases.Split(';', StringSplitOptions.RemoveEmptyEntries |
                                             StringSplitOptions.TrimEntries)
                .ToList()
            : [];
        this.Tag.Notes = !string.IsNullOrWhiteSpace(this.EditingNotes) ? this.EditingNotes : "";
        this.Tag.IsTopLevel = this.EditingIsTopLevel;
        this.OnPropertyChanged(nameof(this.Name));
        this.OnPropertyChanged(nameof(this.Parents));
        this.OnPropertyChanged(nameof(this.Aliases));
        this.OnPropertyChanged(nameof(this.TagBindings));
        this.OnPropertyChanged(nameof(this.Notes));
        this.OnPropertyChanged(nameof(this.IsTopLevel));
        this.OnPropertyChanged(nameof(this.HasChildren));
    }

    public void RefreshParentsString()
    {
        this._isInitialising = true;
        this.OnPropertyChanged(nameof(this.Parents));
        var newParents = this.Parents;
        if (!string.IsNullOrEmpty(newParents))
        {
            this.EditingParents = newParents;
            this.OnPropertyChanged(nameof(this.EditingParents));
        }
        this._isInitialising = false;
    }


    public void SyncChildren(List<TagItemViewModel> children)
    {
        var newChildren = children.Select(c => c.Id).ToHashSet();

        for (var i = this.Children.Count - 1; i >= 0; i--)
            if (!newChildren.Contains(this.Children[i].Id))
                this.Children.RemoveAt(i);

        var currentIds = this.Children.Select(c => c.Id).ToHashSet();
        foreach (var child in children)
            if (!currentIds.Contains(child.Id))
            {
                // Find the correct index to maintain alphabetical order
                var index = 0;
                while (index < this.Children.Count && string.Compare(this.Children[index].Name, child.Name,
                           StringComparison.CurrentCultureIgnoreCase) < 0) index++;

                this.Children.Insert(index, child);
            }
    }

    public void SyncId()
    {
        this.OnPropertyChanged(nameof(this.Id));
        this.OnPropertyChanged(nameof(this.OnDatabase));
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (!this._isInitialising && e.PropertyName!.StartsWith("Editing"))
            this.UserEditedTag?.Invoke(this, EventArgs.Empty);
    }

    private void Validate()
    {
        var parentNames = this.EditingParents.Split(';',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (!this.EditingIsTopLevel && parentNames.Count == 0)
            throw new InvalidOperationException(Resources.ErrorOrphanTagAttempt);

        if (parentNames.Contains(this.Name))
            throw new InvalidOperationException(Resources.ErrorSelfParentAttempt);
    }
}