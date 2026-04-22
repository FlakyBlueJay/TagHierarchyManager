using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;

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

    public event EventHandler? UserEditedTag;

    public string Aliases =>
        this.Tag.Aliases.Count > 0
            ? string.Join("; ", this.Tag.Aliases)
            : string.Empty;

    public bool CanBeDeleted => !this.HasChildren;

    public bool HasChildren => this.CurrentChildren.Count > 0;

    public int Id => this.Tag.Id;

    public string CurrentName => this.Tag.Name;

    public string CurrentNotes => this.Tag.Notes;

    public bool OnDatabase => this.Id != 0;

    public string CurrentTagBindings =>
        this.Tag.TagBindings.Count > 0
            ? string.Join("; ", this.Tag.TagBindings)
            : string.Empty;

    public ObservableCollection<TagItemViewModel> CurrentChildren { get; set; } = [];

    internal Tag Tag { get; set; } = tag;

    private bool IsTopLevel => this.Tag.IsTopLevel;

    internal string CurrentParentsString => this.Tag.Parents is { Count: > 0 }
        ? string.Join("; ", this.Tag.Parents)
        : string.Empty;
    
    private List<int> CurrentParentIds => this.Tag.ParentIds;
    private List<int> EditingParentIds = []; 
    
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
        this.EditingParentIds = this.CurrentParentIds;
        this._isInitialising = false;
    }

    public void CommitEdit()
    {
        this.Validate();
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

        if (string.IsNullOrWhiteSpace(this.EditingName))
            throw new ArgumentException(Resources.ErrorBlankTagName);

        if (!this.EditingIsTopLevel && parentNames.Count == 0)
            throw new InvalidOperationException(Resources.ErrorOrphanTagAttempt);

        if (parentNames.Contains(this.EditingName))
            throw new InvalidOperationException(Resources.ErrorSelfParentAttempt);
    }
}