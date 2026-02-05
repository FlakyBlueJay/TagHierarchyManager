using TagHierarchyManager.Models;

namespace TagHierarchyManager;

// TODO Migrate to RESX
/// <summary>
///     A static class storing error message strings for use when showing errors or throwing exceptions.<br /><br />
///     Some classes can have their own error message classes for more specific messages, this is intended for error
///     messages that are used by multiple classes or do not fit into a specific class.
/// </summary>
public static class ErrorMessages
{
    /// <summary>
    ///     Indicates an attempt to load a file that is not a valid SQLite database.
    /// </summary>
    public const string DbFileNotValid = "This is not a valid SQLite database file.";

    /// <summary>
    ///     Indicates an attempt to use a <see cref="TagDatabase" /> that has not been fully initialised yet.
    /// </summary>
    public const string DbNotInitialised = "The tag hierarchy database has not been initialised.";

    /// <summary>
    ///     Indicates an attempt to load a database with a structure that doesn't match what the application expects.
    /// </summary>
    public const string DbNotValid = "This is not a valid database file for the Tag Hierarchy Manager application.";

    /// <summary>
    ///     Indicates no parameters being received in a method.
    /// </summary>
    public const string EmptyParameters = "No usable parameters received.";

    /// <summary>
    ///     Indicates an attempt to send an empty file path.
    /// </summary>
    public const string FilePathIsEmpty = "File path cannot be empty.";

    /// <summary>
    ///     Indicates an attempt to load a file that does not exist, when loading a <see cref="TagDatabase" />.
    /// </summary>
    public const string FilePathNotFound = "No file exists at the specified path.";

    /// <summary>
    ///     Indicates an attempt to save to/load a file with an extension other than .thdb.
    /// </summary>
    public const string InvalidFileExtension = "File extension must be .thdb.";

    // Settings
    /// <summary>
    ///     Indicates an attempt to delete a setting that is required.
    /// </summary>
    public const string SettingIsRequired = "This setting is required and cannot be deleted.";

    /// <summary>
    ///     Indicates that the specified tag cannot be deleted since it has child tags.
    /// </summary>
    public const string TagHasChildren =
        "The tag requested has child tags and cannot currently be deleted. Delete its children first and try again.";

    /// <summary>
    ///     Indicates that the specified tag doesn't exist in the database.
    /// </summary>
    public const string TagNotFound = "The tag requested does not exist in the database.";

    /// <summary>
    ///     Generates a string indicating that the specified setting already exists in the <see cref="TagDatabase" />.
    /// </summary>
    /// <param name="key">The specified setting key.</param>
    /// <returns>A string indicating the setting already exists.</returns>
    public static string SettingKeyAlreadyExists(string key)
    {
        return $"Setting {key} already exists in the database.";
    }

    /// <summary>
    ///     Generates a string indicating that the specified setting does not exist in the <see cref="TagDatabase" />.
    /// </summary>
    /// <param name="key">The specified setting key.</param>
    /// <returns>A string indicating the setting does not exist.</returns>
    public static string SettingKeyNotFound(string key)
    {
        return $"The setting key \"{key}\" was not found in the settings table.";
    }

    /// <summary>
    ///     Method to construct a string for generic errors from SQLite.
    /// </summary>
    /// <param name="errorCode">The SQLite error code.</param>
    /// <returns>A string indicating that the database couldn't be loaded due to an error in SQLite.</returns>
    public static string SqliteGenericWithCode(int errorCode)
    {
        return $"Cannot load database due to an a SQLite error. Error code: {errorCode}";
    }

    /// <summary>
    ///     Method to construct a string for when attempting to save a new tag that already exists into the TagDatabase.
    /// </summary>
    /// <param name="tagName">The offending name of the tag.</param>
    /// <returns>A string indicating that the specified tag already exists in the TagDatabase.</returns>
    public static string TagAlreadyExists(string tagName)
    {
        return $"Tag \"{tagName}\" already exists in the database.";
    }
}