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
        switch (this.Version)
        {
        case < 2:
            // version 2 adds a date column to tag and removes the aliases table.
            var transaction =
                (SqliteTransaction)
                await this.currentConnection.BeginTransactionAsync().ConfigureAwait(false);
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

            var versionBumpCommand = this.currentConnection.CreateCommand();
            versionBumpCommand.Transaction = transaction;
            versionBumpCommand.CommandText = "UPDATE settings SET value = 2 WHERE key = 'version'";
            versionBumpCommand.ExecuteNonQuery();
            transaction.Commit();
            
            break;
        }

        this.Version = LatestVersion;
    }
}