using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.UI.ViewModels;

public partial class TagItemViewModel(Tag tag, Func<List<int>, List<string>> getParentNamesByIds)
    : ViewModelBase
{
    [ObservableProperty] private string _editingAliases = string.Empty;

    [ObservableProperty] private bool _editingIsTopLevel;

    [ObservableProperty] private string _editingName = tag.Name;

    [ObservableProperty] private string _editingNotes = string.Empty;

    [ObservableProperty] private string _editingParents = string.Empty;

    [ObservableProperty] private string _editingTagBindings = string.Empty;

    private bool _isInitialising;

    public string Aliases =>
        this.Tag.Aliases.Count > 0
            ? string.Join("; ", this.Tag.Aliases)
            : string.Empty;

    public bool CanBeDeleted => !this.HasChildren;

    public string CurrentName => this.Tag.Name;

    public string CurrentNotes => this.Tag.Notes;

    public string CurrentTagBindings =>
        this.Tag.TagBindings.Count > 0
            ? string.Join("; ", this.Tag.TagBindings)
            : string.Empty;

    public bool HasChildren => this.CurrentChildren.Count > 0;

    public int Id => this.Tag.Id;

    public bool OnDatabase => this.Id != 0;

    public ObservableCollection<TagItemViewModel> CurrentChildren { get; set; } = [];

    internal string CurrentParentsString => this.Tag.Parents is { Count: > 0 }
        ? string.Join("; ", this.Tag.Parents)
        : string.Empty;

    internal Tag Tag { get; set; } = tag;

    private bool IsTopLevel => this.Tag.IsTopLevel;


    public void BeginEdit()
    {
        this._isInitialising = true;
        this.EditingName = this.Tag.Name;

        if (this.OnDatabase || string.IsNullOrEmpty(this.EditingParents))
            this.EditingParents = this.CurrentParentsString;

        this.EditingIsTopLevel = this.IsTopLevel;
        this.EditingTagBindings = this.CurrentTagBindings;
        this.EditingAliases = this.Aliases;
        this.EditingNotes = this.CurrentNotes;
        this._isInitialising = false;
    }

    public void CommitEdit(Tag savedTag)
    {
        this.Tag = savedTag;
        this.OnPropertyChanged(nameof(this.Id));
        this.OnPropertyChanged(nameof(this.OnDatabase));
        this.OnPropertyChanged(nameof(this.CurrentName));
        this.OnPropertyChanged(nameof(this.CurrentParentsString));
        this.OnPropertyChanged(nameof(this.Aliases));
        this.OnPropertyChanged(nameof(this.CurrentTagBindings));
        this.OnPropertyChanged(nameof(this.CurrentNotes));
        this.OnPropertyChanged(nameof(this.IsTopLevel));
        this.OnPropertyChanged(nameof(this.HasChildren));
    }

    public void RefreshParentsString()
    {
        this._isInitialising = true;
        this.OnPropertyChanged(nameof(this.CurrentParentsString));
        var newParents = this.CurrentParentsString;
        if (!string.IsNullOrEmpty(newParents))
        {
            this.EditingParents = newParents;
            this.OnPropertyChanged(nameof(this.EditingParents));
        }

        this._isInitialising = false;
    }

    public void RefreshSelf()
    {
        this.OnPropertyChanged(nameof(this.CurrentName));
        this.OnPropertyChanged(nameof(this.CurrentParentsString));
        this.OnPropertyChanged(nameof(this.Aliases));
        this.OnPropertyChanged(nameof(this.CurrentTagBindings));
        this.OnPropertyChanged(nameof(this.CurrentNotes));
        this.OnPropertyChanged(nameof(this.IsTopLevel));
        this.OnPropertyChanged(nameof(this.HasChildren));
    }

    public void UpdateTag(Tag tag)
    {
        this.Tag = tag;
    }
}