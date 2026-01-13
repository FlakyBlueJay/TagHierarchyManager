using Avalonia.Platform.Storage;

namespace TagHierarchyManager.UI.Views;

public class Common
{
    public static FilePickerFileType MusicBeeTagHierarchy { get; } = new(Assets.Resources.FileFormatMusicBeeTagHierarchy)
    {
        Patterns = ["*.txt"]
    };

    public static FilePickerFileType TagDatabaseFileType { get; } = new(Assets.Resources.FileFormatTagHierarchyDatabase)
    {
        Patterns = ["*.thdb"]
    };
}