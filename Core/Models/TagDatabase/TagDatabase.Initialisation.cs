using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    private const string InMemoryDbName = "(temporary in-memory database)";
    private const string InMemoryDbPath = ":memory:";
    private const string TagHierarchyDbFileExt = ".thdb";


    /// <summary>
    ///     Throws an exception if the database has not been initialised yet.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the database has not been initialised or currentConnection is
    ///     null.
    /// </exception>
    [MemberNotNull(nameof(currentConnection), nameof(Connection))]
    private void CheckInitialisation()
    {
        if (!this.Initialised || this.currentConnection is null || this.Connection is null)
            throw new InvalidOperationException(ErrorMessages.DbNotInitialised);
    }

    /// <summary>
    ///     Checks if the current connection exists and is open, and closes it.
    /// </summary>
    public void Close()
    {
        if (this.currentConnection?.State != ConnectionState.Open) return;
        this.currentConnection.Close();
        this.currentConnection.Dispose();
        this.Initialised = false;
    }

    /// <summary>
    ///     Creates the database, fills the settings, then initialises the database object.
    /// </summary>
    /// <param name="filePath">Path to the database file.</param>
    /// <param name="overwrite">Whether to overwrite the existing file.</param>
    /// <param name="tagsToImport">A dictionary of tags to import.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    public async Task CreateAsync(string filePath, bool overwrite = false,
        Dictionary<string, ImportedTag>? tagsToImport = null)
    {
        await this.CreateDatabaseAsync(filePath, overwrite, tagsToImport).ConfigureAwait(false);
    }

    /// <summary>
    ///     Loads the database from an existing file.
    /// </summary>
    /// <param name="filePath">Path to the database file.</param>
    /// <param name="connection">The <see cref="SqliteConnection" /> to use, instead of a filePath.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation that returns a TagDatabase.</returns>
    public async Task LoadAsync(string filePath = "", SqliteConnection? connection = null)
    {
        await this.LoadDatabaseAsync(filePath, connection).ConfigureAwait(false);
    }

    private static string? ValidateFilePath(string filePath, bool loadMode = false)
    {
        if (filePath == InMemoryDbPath) return null;

        if (string.IsNullOrEmpty(filePath)) return ErrorMessages.FilePathIsEmpty;

        string fileExt = Path.GetExtension(filePath);
        if (fileExt != TagHierarchyDbFileExt) return ErrorMessages.InvalidFileExtension;

        if (loadMode && !File.Exists(filePath)) return ErrorMessages.FilePathNotFound;

        return null;
    }

    private async Task CreateDatabaseAsync(string filePath, bool overwrite = false,
        Dictionary<string, ImportedTag>? tagsToImport = null)
    {
        try
        {
            if (overwrite && File.Exists(filePath)) File.Delete(filePath);

            string? errorString = ValidateFilePath(filePath);
            if (errorString is not null)
            {
                throw new ArgumentException(errorString);
            }

            this.currentConnection = new SqliteConnection($"Data Source={filePath};Pooling=False");

            await this.currentConnection.OpenAsync().ConfigureAwait(false);
            SqliteCommand command = this.currentConnection.CreateCommand();
            
            command.CommandText = """
                                  CREATE TABLE "tag" (
                                      "id"                INTEGER NOT NULL,
                                      "name"              TEXT NOT NULL UNIQUE,
                                      "notes"             TEXT DEFAULT '',
                                      "top_level"         INTEGER NOT NULL DEFAULT 0,
                                      "tags_to_bind"      TEXT,
                                      "also_known_as"     TEXT DEFAULT '',
                                      PRIMARY KEY("id" AUTOINCREMENT)
                                  );

                                  CREATE TABLE "alias" (
                                      "id"                INTEGER NOT NULL,
                                      "tag_id"            INTEGER NOT NULL,
                                      "name"              TEXT,
                                      PRIMARY KEY("id" AUTOINCREMENT),
                                      FOREIGN KEY("tag_id") REFERENCES "tag"("id") ON DELETE CASCADE
                                  );

                                  CREATE TABLE "tag_parent_link" (
                                      "target_tag_id" INT NOT NULL,
                                      "parent_tag_id" INT NOT NULL CHECK("parent_tag_id" != "target_tag_id"),
                                      FOREIGN KEY("parent_tag_id") REFERENCES "tag"("id") ON DELETE CASCADE,
                                      FOREIGN KEY("target_tag_id") REFERENCES "tag"("id") ON DELETE CASCADE
                                  );

                                  CREATE TABLE "settings" (
                                      "key" TEXT NOT NULL UNIQUE,
                                      "value" TEXT NOT NULL
                                  );

                                  INSERT INTO "main"."settings" ("key", "value") VALUES ('version', '1');
                                  INSERT INTO "main"."settings" ("key", "value") VALUES ('default_tag_bind', 'genre');

                                  CREATE TRIGGER DoNotChangeRequiredKeys BEFORE UPDATE ON settings
                                  FOR EACH ROW
                                  WHEN OLD.key IN ('version', 'default_tag_bind') AND OLD.key != NEW.key
                                  BEGIN      
                                  SELECT RAISE(ABORT,'CANNOT_CHANGE_REQUIRED_KEY'); 
                                  END;

                                  CREATE TRIGGER DoNotDeleteRequired
                                  BEFORE DELETE ON settings
                                  FOR EACH ROW
                                  WHEN OLD.key IN ('version', 'default_tag_bind')
                                  BEGIN
                                  SELECT RAISE(ABORT, 'CANNOT_DELETE_REQUIRED_KEY');
                                  END;
                                  """;
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            await this.FinishInitialisationAsync(tagsToImport).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "An error occurred: {ErrorMessage} ", ex.Message);
            throw;
        }
    }

    /// <summary>
    ///     Initialises the tag hierarchy database.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    private async Task FinishInitialisationAsync(Dictionary<string, ImportedTag>? tagsToImport = null)
    {
        this.Logger.Information("[TagHierarchyDatabase.Initialise] Initialising...");
        SqliteCommand command = this.currentConnection?.CreateCommand() ??
                                throw new InvalidOperationException(ErrorMessages.DbNotInitialised);
        command.CommandText = "SELECT * FROM SETTINGS;";
        try
        {
            await using (SqliteDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    string currentSetting = reader.GetString(0);
                    switch (currentSetting)
                    {
                        case "version":
                            this.Version = Convert.ToInt16(reader.GetString(1), CultureInfo.InvariantCulture);
                            break;
                        case "default_tag_bind":
                            this.DefaultTagBindings = reader.GetString(1)
                                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .ToList();
                            break;
                    }
                }
            }

            this.FilePath = this.currentConnection.DataSource;
            this.Name = this.currentConnection.DataSource != InMemoryDbPath
                ? Path.GetFileNameWithoutExtension(this.currentConnection.DataSource)
                : InMemoryDbName;

            this.Initialised = true;


            if (tagsToImport is not null)
                try
                {
                    await this.ImportAsync(tagsToImport).ConfigureAwait(false);
                }
                catch
                {
                    this.Close();
                    throw;
                }

            if (tagsToImport is null)
                this.Tags = await this.GetAllTagsFromDatabase();

            this.OnInitialisationComplete(EventArgs.Empty);
            Debug.WriteLine(
                $"[TagHierarchyDatabase.Initialise] Successfully initialised: Version {this.Version}, Name: {this.Name}, Default binding string: {this.DefaultTagBindings}");
        }
        catch (SqliteException ex)
        {
            this.Logger.Error(ex, "An error occurred: {ErrorMessage} ", ex.Message);
            throw;
        }
    }

    private async Task LoadDatabaseAsync(string filePath, SqliteConnection? connection = null)
    {
        if (connection is null)
        {
            string? errorString = ValidateFilePath(filePath, true);
            if (errorString is not null)
            {
                throw new ArgumentException(errorString);
            }
        }

        this.currentConnection = connection ?? new SqliteConnection($"Data Source={filePath};Pooling=False");
        if (connection is null) await this.currentConnection.OpenAsync().ConfigureAwait(false);

        this.Logger.Debug("[TagDatabaseObject.Load] Connection opened: {@FilePath}", filePath);

        if (await this.ValidateAsync(this.currentConnection).ConfigureAwait(false))
            await this.FinishInitialisationAsync().ConfigureAwait(false);
    }

    private async Task<bool> ValidateAsync(SqliteConnection connection)
    {
        bool sqliteDatabaseCheck = await this.ValidateFileIsSqliteDatabaseAsync(connection).ConfigureAwait(false);
        bool structureCheck = await this.ValidateDatabaseStructureAsync(connection).ConfigureAwait(false);

        return sqliteDatabaseCheck && structureCheck;
    }

    private async Task<bool> ValidateDatabaseStructureAsync(SqliteConnection connection)
    {
        try
        {
            SqliteCommand tableCheckCommand = connection.CreateCommand();
            tableCheckCommand.CommandText = """
                                                SELECT name FROM sqlite_master
                                                WHERE name != 'sqlite_sequence'
                                                AND type == 'table'
                                                ORDER BY name
                                            """;
            this.Logger.Debug("[TagDatabaseObject.Load] Command created");
            bool notTagDatabase = false;
            await using (SqliteDataReader reader = await tableCheckCommand.ExecuteReaderAsync().ConfigureAwait(false))
            {
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    if (notTagDatabase) continue;
                    string currentTable = reader.GetString(0);
                    HashSet<string> allowedTables = ["tag", "tag_parent_link", "alias", "settings"];
                    if (!allowedTables.Contains(currentTable)) notTagDatabase = true;
                }
            }

            if (notTagDatabase)
            {
                this.Close();
                throw new ArgumentException(ErrorMessages.DbNotValid);
            }
        }
        catch (SqliteException ex)
        {
            this.Close();
            if (ex.SqliteErrorCode == 1)
                throw new ArgumentException(ErrorMessages.DbNotValid);
            throw;
        }

        return true;
    }

    private async Task<bool> ValidateFileIsSqliteDatabaseAsync(SqliteConnection connection)
    {
        try
        {
            SqliteCommand validityCheck = connection.CreateCommand();
            validityCheck.CommandText = "pragma schema_version;";
            int? schemaVersion = Convert.ToInt32(await validityCheck.ExecuteScalarAsync().ConfigureAwait(false),
                CultureInfo.InvariantCulture);
            if (schemaVersion == 0)
            {
                throw new ArgumentException(ErrorMessages.DbFileNotValid);
            }
        }
        catch (SqliteException ex)
        {
            this.Close();
            if (ex.SqliteErrorCode == 26)
                throw new ArgumentException(ErrorMessages.DbFileNotValid);
            throw;
        }

        return true;
    }
}