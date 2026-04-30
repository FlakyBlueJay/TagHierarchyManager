using Microsoft.Data.Sqlite;
using TagHierarchyManager.Assets;
using TagHierarchyManager.Utilities;

namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    private async Task ImportAsync(Dictionary<string, ImportedTag> importDict)
    {
        if (this.currentConnection is null)
            throw new InvalidOperationException(ErrorMessages.TagDatabaseNotInitialised);
        
        await using SqliteTransaction transaction =
            (SqliteTransaction)await this.currentConnection.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            // phase 1: add all the tags, without anything that relies on other tables.
            foreach (ImportedTag tag in importDict.Values) await this.WriteImportedTagToDatabase(transaction, tag);

            this.Tags = await this.GetAllTagsFromDatabase(transaction: transaction).ConfigureAwait(false);

            // phase 2: add the parents and aliases.
            foreach (ImportedTag tag in importDict.Values)
            {
                Tag? currentTag = this.Tags.SingleOrDefault(t => t.Name == tag.Name);
                if (currentTag is null)
                {
                    //var currentTagList = await this.SelectTagsFromDatabase(tag.Name).ConfigureAwait(false);
                    //currentTag = currentTagList[0];
                }
                if (currentTag is null)
                    throw new InvalidOperationException(ErrorMessages.TagDatabaseTagNotFound);
                // todo search parent here then save the parents.
                await this.SaveTagParents(transaction, currentTag.Id, tag.Parents, currentTag).ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }


    private async Task WriteImportedTagToDatabase(SqliteTransaction transaction, ImportedTag tag)
    {
        if (this.currentConnection is null)
            throw new InvalidOperationException(ErrorMessages.TagDatabaseNotInitialised);
        
        SqliteCommand addCommand = this.currentConnection.CreateCommand();
        addCommand.Transaction = transaction;
        addCommand.CommandText = """
                                 INSERT INTO tag (name, notes, top_level, tags_to_bind, also_known_as, date_modified)
                                 VALUES (@name, @notes, @is_top_level, @tags_to_bind, @aliases, CURRENT_TIMESTAMP)
                                 """;
        addCommand.Parameters.AddWithValue("@name", tag.Name);
        addCommand.Parameters.AddWithValue("@notes", tag.Notes);
        addCommand.Parameters.AddWithValue("@is_top_level", tag.IsTopLevel ? 1 : 0);
        addCommand.Parameters.AddWithValue("@tags_to_bind", string.Join(";", tag.TagBindings));
        addCommand.Parameters.AddWithValue("@aliases", string.Join(";", tag.Aliases));
        await addCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}