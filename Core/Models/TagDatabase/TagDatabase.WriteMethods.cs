using System.Globalization;
using Microsoft.Data.Sqlite;
using TagHierarchyManager.Utilities;

namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    /// <summary>
    ///     Saves all tag objects to the database.
    /// </summary>
    /// <param name="tags">The List of Tag objects.</param>
    /// <param name="transaction">The SqliteTransaction to execute queries on, will make its own if null.</param>
    /// <exception cref="ArgumentException">Thrown if the tag already existed in the database.</exception>
    public async Task WriteTagsToDatabase(List<Tag> tags, SqliteTransaction? transaction = null)
    {
        this.CheckInitialisation();
        bool isTransactionOwner = transaction == null;
        transaction ??= (SqliteTransaction)await this.currentConnection.BeginTransactionAsync().ConfigureAwait(false);

        List<Tag> updatedTags = [];
        List<Tag> newlyAddedTags = [];

        try
        {
            foreach (var tag in tags)
            {
                bool alreadyOnDatabase = tag.Id != 0;
                
                SqliteCommand addCommand = this.currentConnection.CreateCommand();
                addCommand.Transaction = transaction;
                QueryProcessorHandler.ProcessTagSaveCommand(addCommand, tag);
                
                if (await this.SelectTagFromDatabase(tag.Name) is not null && !alreadyOnDatabase)
                    throw new ArgumentException(ErrorMessages.TagAlreadyExists(tag.Name));
                
                tag.Id = Convert.ToInt32(await addCommand.ExecuteScalarAsync().ConfigureAwait(false),
                    CultureInfo.InvariantCulture);
                
                await this.SaveTagParents(transaction, tag.Id, tag.Parents, tag).ConfigureAwait(false);

                int index = this.Tags.FindIndex(t => t.Id == tag.Id);
                if (index != -1)
                {
                    this.Tags[index] = tag;
                    updatedTags.Add(tag);
                }
                else
                {
                    this.Tags.Add(tag);
                    newlyAddedTags.Add(tag);
                }
            }
            
            if (isTransactionOwner) await transaction.CommitAsync().ConfigureAwait(false);

            TagsWritten?.Invoke(this, new DatabaseEditResult(newlyAddedTags, updatedTags, []));
        }
        catch (SqliteException)
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (isTransactionOwner) await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }
    
    private async Task SaveTagParents(SqliteTransaction transaction, int id, IReadOnlyCollection<string> parents,
        Tag? tag = null)
    {
        if (parents.Count == 0) return;
        this.CheckInitialisation();

        List<int> parentIds = [];

        // process parents, grabbing the names first in case the user wants to change the parents.
        foreach (string parentName in parents)
        {
            Tag? retrievedTag = this.Tags.SingleOrDefault(t => t.Name == parentName)
                                ?? await this.SelectTagFromDatabase(parentName).ConfigureAwait(false);
            if (retrievedTag is null) throw new ArgumentException(ErrorMessages.TagNotFound);

            parentIds.Add(retrievedTag.Id);
        }

        // clear existing tag parents so we have a clean slate.
        SqliteCommand deleteCommand = this.currentConnection.CreateCommand();
        deleteCommand.Transaction = transaction;
        deleteCommand.CommandText = """
                                        DELETE FROM tag_parent_link
                                        WHERE target_tag_id = @tag_id
                                    """;
        deleteCommand.Parameters.AddWithValue("@tag_id", id);
        await deleteCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        // process + add parent links
        SqliteCommand parentCommand = this.currentConnection.CreateCommand();
        parentCommand.Transaction = transaction;
        parentCommand.CommandText = """
                                        INSERT INTO tag_parent_link (target_tag_id, parent_tag_id)
                                        VALUES (@target_tag_id, @parent_tag_id)
                                    """;
        parentCommand.Parameters.Clear();
        parentCommand.Parameters.AddWithValue("@target_tag_id", id);
        parentCommand.Parameters.Add("@parent_tag_id", SqliteType.Integer);
        await parentCommand.PrepareAsync();

        foreach (int parentId in parentIds)
        {
            parentCommand.Parameters["@parent_tag_id"].Value = (long)parentId;
            await parentCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        // add tag parent IDs to tag if the tag was provided.
        if (tag is not null) tag.ParentIds = parentIds;
    }
}