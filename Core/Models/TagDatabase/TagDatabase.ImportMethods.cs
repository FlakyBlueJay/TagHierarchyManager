using Microsoft.Data.Sqlite;
using TagHierarchyManager.Assets;

namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    private async Task ImportAsync(Dictionary<string, ImportedTag> importDict)
    {
        if (this.currentConnection is null)
            throw new InvalidOperationException(ErrorMessages.TagDatabaseNotInitialised);

        await using var transaction =
            (SqliteTransaction)await this.currentConnection.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            // phase 1: add all the tags, without anything that relies on other tables.
            var nameToId = new Dictionary<string, int>();
            foreach (var tag in importDict.Values)
            {
                var id = await this.WriteImportedTagToDatabase(transaction, tag);
                nameToId[tag.Name] = id;
            }

            this.Tags = await this.GetAllTagsFromDatabase(transaction: transaction).ConfigureAwait(false);

            // phase 2: add the parents and aliases.
            foreach (var tag in importDict.Values)
            {
                var currentTag = this.Tags.SingleOrDefault(t => t.Name == tag.Name);

                var parentIds = tag.Parents.Select(parentName => nameToId[parentName]).ToList();

                if (parentIds.Count == 0) continue;
                await this.WriteImportedParentsToDatabase(transaction, currentTag.Id, parentIds).ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task WriteImportedParentsToDatabase(SqliteTransaction transaction, int targetId, List<int> parentIds)
    {
        var valuesClauses = new List<string>();
        var parameters = new List<SqliteParameter>();

        for (var i = 0; i < parentIds.Count; i++)
        {
            valuesClauses.Add($"(@target_tag_id_{i}, @parent_tag_id_{i})");
            parameters.Add(new SqliteParameter($"@target_tag_id_{i}", targetId));
            parameters.Add(new SqliteParameter($"@parent_tag_id_{i}", parentIds[i]));
        }

        var query =
            $"INSERT INTO tag_parent_link (target_tag_id, parent_tag_id) VALUES {string.Join(", ", valuesClauses)};";

        var parentCommand = this.currentConnection.CreateCommand();
        parentCommand.Transaction = transaction;
        parentCommand.CommandText = query;
        parentCommand.Parameters.AddRange(parameters);
        await parentCommand.PrepareAsync();
        await parentCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
    }


    private async Task<int> WriteImportedTagToDatabase(SqliteTransaction transaction, ImportedTag tag)
    {
        if (this.currentConnection is null)
            throw new InvalidOperationException(ErrorMessages.TagDatabaseNotInitialised);

        var addCommand = this.currentConnection.CreateCommand();
        addCommand.Transaction = transaction;
        addCommand.CommandText = """
                                 INSERT INTO tag (name, notes, top_level, tags_to_bind, also_known_as, date_modified)
                                 VALUES (@name, @notes, @is_top_level, @tags_to_bind, @aliases, CURRENT_TIMESTAMP)
                                 RETURNING last_insert_rowid();
                                 """;
        addCommand.Parameters.AddWithValue("@name", tag.Name);
        addCommand.Parameters.AddWithValue("@notes", tag.Notes);
        addCommand.Parameters.AddWithValue("@is_top_level", tag.IsTopLevel ? 1 : 0);
        addCommand.Parameters.AddWithValue("@tags_to_bind", string.Join(";", tag.TagBindings));
        addCommand.Parameters.AddWithValue("@aliases", string.Join(";", tag.Aliases));
        var newId = Convert.ToInt32(await addCommand.ExecuteScalarAsync().ConfigureAwait(false));
        return newId;
    }
}