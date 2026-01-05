using System.Collections.ObjectModel;
using System.Linq;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.UI.ViewModels;

public class TagItemViewModel(Tag tag) : ViewModelBase
{
    private Tag Tag { get; } = tag;

    public int Id => Tag.Id;

    public string Name
    {
        get => Tag.Name;
        set
        {
            if (Tag.Name == value) return;
            Tag.Name = value;
            OnPropertyChanged();
        }
    }
    
    // parents are saved in the UI using a semi-colon separated string.
    public string Parents
    {
        get => Tag.Parents.Count > 0 ? string.Join("; ", Tag.Parents) : string.Empty;
        set
        {
            if (!Tag.Parents.SequenceEqual(value.Split(";"))) Tag.Parents = value.Split(";").ToList();
            OnPropertyChanged();
        }
    }
    
    public ObservableCollection<TagItemViewModel> Children { get; } = [];
}