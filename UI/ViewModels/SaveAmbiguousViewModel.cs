using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI.ViewModels;

public partial class SaveAmbiguousViewModel : ViewModelBase
{
    
    [ObservableProperty] private Tag _currentTag;
    
    [ObservableProperty] private List<TagItemViewModel> _tags;

    [ObservableProperty] private TagItemViewModel? _selectedTag;

    [ObservableProperty] private string _dialogBody;

    public SaveAmbiguousViewModel(TagDatabaseService tagDatabaseService, Tag currentTag,
        List<Tag> tags)
    {
        this._tags = tags.Select(t => new TagItemViewModel(t, tagDatabaseService.GetParentNamesByIds)).ToList();
        this._currentTag = currentTag;
        this._dialogBody = string.Format(Resources.DialogSaveAmbiguousBody, currentTag.Name);
    }
    
}