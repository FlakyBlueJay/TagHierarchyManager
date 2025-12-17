using System.Globalization;
using Microsoft.Data.Sqlite;
using Serilog;

namespace TagHierarchyManager.Models;

/// <summary>
///     An object representing a Tag Hierarchy Manager database file.
/// </summary>
public partial class TagDatabase
{
    private const string AliasesColumnName = "also_known_as";
    private const string IdColumnName = "id";
    private const string NameColumnName = "name";
    private const string NotesColumnName = "notes";
    private const string ParentIdsColumnName = "parent_ids";
    private const string TagBindingsColumnName = "tags_to_bind";
    private const string TopLevelColumnName = "top_level";
    private SqliteConnection? currentConnection;

    private List<string> defaultBindings = ["genre"];

    /// <summary>
    ///     Initializes a new instance of the <see cref="TagDatabase" /> class.
    /// </summary>
    public TagDatabase()
    {
        this.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        this.Settings = new SettingsHandler(this);
    }

    /// <summary>
    ///     Gets a <see cref="SettingsHandler" /> for manipulating settings at a lower level.
    /// </summary>
    public SettingsHandler Settings { get; }

    /// <summary>
    ///     Gets or sets a List(string) of tag binding(s) that will be added to a tag by default. Defaults to <i>genre</i>
    ///     (e.g. Festival Progressive House <i>::genre</i>).
    /// </summary>
    public List<string> DefaultTagBindings
    {
        get => this.defaultBindings;
        set
        {
            if (this.defaultBindings == value) return;
            this.defaultBindings = value;
            if (this.Initialised)
                _ = this.Settings.UpdateSettingAsync(SettingsHandler.DefaultTagBindingKey, string.Join(';', value));
        }
    }

    /// <summary>
    ///     Gets the location of the .thdb file associated with the <see cref="TagDatabase" />, inferred from the current
    ///     connection's data source on initialisation.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? FilePath { get; private set; }

    /// <summary>
    ///     Gets the name of the database, inferred from the source file's name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    ///     Gets the list of <see cref="Tag" />s in the database.
    /// </summary>
    public List<Tag> Tags { get; private set; } = [];

    /// <summary>
    ///     Gets version of the database. Cannot be set outside of initialisation.
    /// </summary>
    public int Version { get; private set; }
    
    /// <summary>
    ///     Gets the SQLite connection associated with the <see cref="TagDatabase" />.
    /// </summary>
    private SqliteConnection? Connection => this.currentConnection;

    /// <summary>
    ///     Gets a Logger object, using the Serilog library.
    /// </summary>
    private ILogger Logger { get; }

    /// <summary>
    ///     Gets a value indicating whether the database has been initialised or not.
    /// </summary>
    private bool Initialised { get; set; }
}