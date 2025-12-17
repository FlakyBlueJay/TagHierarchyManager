namespace TagHierarchyManager.Common;

/// <summary>
///     A static class for grabbing file types, for both the core and the user interfaces.
/// </summary>
public static class FileTypes
{
    /// <summary>
    ///     Gets a list of all file types that are not tag hierarchy databases.
    /// </summary>
    public static List<(string FileExtension, string Name)> AllNonDatabaseFileTypes =>
    [
        MusicBeeTagHierarchyTemplate,
    ];

    /// <summary>
    ///     Gets the file type metadata for the MusicBee tag hierarchy template format (*.txt).
    /// </summary>
    /// Note that if .txt is used for other formats in the future, the user will have to be prompted on which format to
    /// use.
    public static (string FileExtension, string Name) MusicBeeTagHierarchyTemplate =>
        new(".txt", "MusicBee tag hierarchy template");

    /// <summary>
    ///     Gets the file type metadata for the Tag Hierarchy Manager database file (*.thdb).
    /// </summary>
    public static (string FileExtension, string Name) TagDatabase =>
        new(".thdb", "Tag Hierarchy Manager hierarchy database");
}