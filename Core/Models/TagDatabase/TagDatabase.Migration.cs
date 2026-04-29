using Microsoft.Data.Sqlite;

namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    /// <summary>
    /// Performs necessary migrations from older database versions.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the database's current connection is null.</exception>
    private async Task PerformNeededMigrations()
    {
        if (this.currentConnection is null)
            throw new InvalidOperationException(ErrorMessages.DbNotInitialised);
        var transaction =
            (SqliteTransaction)
            await this.currentConnection.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            if (this.Version < 2)
            {
                // version 2 adds a date column to tag and removes the aliases table.
                var addDateAddedCommand = this.currentConnection.CreateCommand();
                addDateAddedCommand.Transaction = transaction;
                addDateAddedCommand.CommandText =
                    $"""
                     ALTER TABLE tag ADD COLUMN date_created DATETIME DEFAULT NULL;
                     ALTER TABLE tag ADD COLUMN date_modified DATETIME DEFAULT NULL;
                     """;
                addDateAddedCommand.ExecuteNonQuery();

                var updateModifiedCommand = this.currentConnection.CreateCommand();
                updateModifiedCommand.Transaction = transaction;
                updateModifiedCommand.CommandText = "UPDATE tag SET date_modified = CURRENT_TIMESTAMP;";
                updateModifiedCommand.ExecuteNonQuery();

                var deleteOldTableCommand = this.currentConnection.CreateCommand() ??
                                            throw new InvalidOperationException(ErrorMessages.DbNotInitialised);
                deleteOldTableCommand.Transaction = transaction;
                deleteOldTableCommand.CommandText =
                    "DROP TABLE alias;";
                deleteOldTableCommand.ExecuteNonQuery();
            }

            if (this.Version < 3)
            {
                // version 3 removes the unique constraint on the name column in the tag table.
                // disable foreign keys first
                var disableForeignKeysCommand = this.currentConnection.CreateCommand();
                disableForeignKeysCommand.Transaction = transaction;
                disableForeignKeysCommand.CommandText = "PRAGMA foreign_keys=OFF;";
                disableForeignKeysCommand.ExecuteNonQuery();

                // recreate tag table with non-unique constraints.
                var nonUniqueCommand = this.currentConnection.CreateCommand();
                nonUniqueCommand.Transaction = transaction;
                nonUniqueCommand.CommandText =
                    $"""
                     CREATE TABLE "tag_new" (
                         "id"                INTEGER PRIMARY KEY AUTOINCREMENT,
                         "name"              TEXT NOT NULL,
                         "notes"             TEXT DEFAULT '',
                         "top_level"         INTEGER NOT NULL DEFAULT 0,
                         "tags_to_bind"      TEXT DEFAULT '',
                         "also_known_as"     TEXT DEFAULT '',
                         "date_created"      DATETIME,
                         "date_modified"     DATETIME
                     );

                     INSERT INTO tag_new SELECT * FROM tag;

                     DROP TABLE tag;

                     ALTER TABLE tag_new RENAME TO tag;
                     PRAGMA foreign_keys=ON;
                     """;
                nonUniqueCommand.ExecuteNonQuery();
                var reenableForeignKeysCommand = this.currentConnection.CreateCommand();
                reenableForeignKeysCommand.CommandText = "PRAGMA foreign_keys=ON;";
                reenableForeignKeysCommand.ExecuteNonQuery();
            }

            var versionBumpCommand = this.currentConnection.CreateCommand();
            versionBumpCommand.CommandText = "UPDATE settings SET value = @newVersion WHERE key = 'version'";
            versionBumpCommand.Parameters.AddWithValue("@newVersion", LatestVersion);
            versionBumpCommand.ExecuteNonQuery();
            await transaction.CommitAsync().ConfigureAwait(false);
            this.Version = LatestVersion;
        }
        catch
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            var forceEnableFkCommand = this.currentConnection.CreateCommand();
            forceEnableFkCommand.CommandText = "PRAGMA foreign_keys = ON;";
            await forceEnableFkCommand.ExecuteNonQueryAsync();
        }
    }
}