using Avalonia.Platform.Storage;
using TagHierarchyManager.UI.Assets;

namespace TagHierarchyManager.UI;

public static class Common
{
    public static FilePickerFileType MusicBeeTagHierarchy { get; } = new(Resources.FileFormatMusicBeeTagHierarchy)
    {
        Patterns = ["*.txt"]
    };

    public static FilePickerFileType TagDatabaseFileType { get; } = new(Resources.FileFormatTagHierarchyDatabase)
    {
        Patterns = ["*.thdb"]
    };
}