using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.UI.ViewModels;

public partial class SaveAmbiguousViewModel : ViewModelBase
{
    
    [ObservableProperty] private Tag _currentTag;
    
    [ObservableProperty] private List<TagItemViewModel> _tags;

    [ObservableProperty] private TagItemViewModel? _selectedTag;

    public SaveAmbiguousViewModel(TagDatabaseService tagDatabaseService, Tag currentTag,
        List<Tag> tags)
    {
        this._tags = tags.Select(t => new TagItemViewModel(t, tagDatabaseService.GetParentNamesByIds)).ToList();
        this._currentTag = currentTag;
    }
    
}