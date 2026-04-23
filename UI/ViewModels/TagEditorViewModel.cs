using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;
using TagHierarchyManager.UI.Views;

namespace TagHierarchyManager.UI.ViewModels;

public partial class TagEditorViewModel(MainWindowViewModel mainWindow) : ViewModelBase, IDisposable
{
    private readonly MainWindowViewModel? _mainWindow = mainWindow;
    [ObservableProperty] private TagItemViewModel? _selectedTag;

    [ObservableProperty] private int _selectedTagId;
    [ObservableProperty] private bool _unsavedChanges;

    public bool CanDeleteSelectedTag => this._mainWindow?.SelectedTag is not null
                                        && this._mainWindow.SelectedTag.Id > 0
                                        && this.TagDatabaseService?
                                            .GetAllTagChildren(this._mainWindow.SelectedTag.Id).Count == 0;

    private TagDatabaseService? TagDatabaseService => this._mainWindow?.TagDatabaseService;

    public void Dispose()
    {
        if (this._mainWindow is null || this.TagDatabaseService is null) return;
        if (!this.TagDatabaseService.IsDatabaseOpen) return;

        GC.SuppressFinalize(this);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    internal async Task NewTag()
    {
        if (this._mainWindow is null || this.TagDatabaseService is null) return;
        var userWantsToSave = await this._mainWindow.ShowUnsavedChangesDialog();
        if (userWantsToSave is null) return;

        this._mainWindow.SelectedTag = new TagItemViewModel(
            new Tag {
                Name = string.Empty,
                IsTopLevel = true,
                TagBindings = this.TagDatabaseService.DefaultTagBindings
            },
            this.TagDatabaseService.GetParentNamesByIds
        );
        this.UnsavedChanges = true;
    }

    [RelayCommand]
    internal async Task SaveSelectedTagAsync()
    {
        try
        {
            if (this._mainWindow is null || this.TagDatabaseService is null) return;
            if (this._mainWindow.SelectedTag is null || !this.TagDatabaseService.IsDatabaseOpen ||
                !this._mainWindow.IsDbEnabled) return;
            await this.TagDatabaseService.WriteTagsToDatabase([this._mainWindow.SelectedTag]);
            this._mainWindow.SelectedTag.RefreshParentsString();
            this._mainWindow.StatusBlockText = string.Format(Resources.StatusBlockTagSaveSuccessful,
                this._mainWindow.SelectedTag.CurrentName);
            this.UnsavedChanges = false;
        }
        catch (Exception ex)
        {
            this._mainWindow?.ShowErrorDialog(ex.Message);
        }
    }

    [RelayCommand]
    private async Task CancelTagEditAsync()
    {
        if (this._mainWindow is null) return;
        if (this.UnsavedChanges)
        {
            var userWantsToOverwrite = await this._mainWindow.ShowNullableBoolDialog(new UnsavedCancelDialog());
            if (userWantsToOverwrite is not null && (bool)!userWantsToOverwrite) return;
        }

        this._mainWindow.SelectedTag?.BeginEdit();
        this.UnsavedChanges = false;
    }

    [RelayCommand]
    private void FlagUnsavedChanges()
    {
        this.UnsavedChanges = true;
    }

    partial void OnSelectedTagChanged(TagItemViewModel? value)
    {
        // MainWindowViewModel handles the actual main tag.
        // When SelectedTag here is changed, it's assumed the user has decided to change the tag.

        if (value is null) return;
        value.BeginEdit();
        this.UnsavedChanges = false;
    }

    partial void OnSelectedTagIdChanged(int value)
    {
        if (value is 0 || this._mainWindow is null || this.TagDatabaseService is null) return;
        var tag = this.TagDatabaseService.GetTagById(value);
        this._mainWindow.SelectedTag =
            tag is null
                ? null
                : new TagItemViewModel(tag, this.TagDatabaseService.GetParentNamesByIds);
    }


    [RelayCommand]
    private async Task StartTagDeletionAsync()
    {
        if (this._mainWindow?.SelectedTag is null || !this.CanDeleteSelectedTag || this._mainWindow is null) return;
        await this._mainWindow.StartTagDeletionAsync(this._mainWindow.SelectedTag.Id);
    }
}