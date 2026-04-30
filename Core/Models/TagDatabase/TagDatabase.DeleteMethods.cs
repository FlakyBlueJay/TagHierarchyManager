using System.Diagnostics;
using Microsoft.Data.Sqlite;
using TagHierarchyManager.Assets;

namespace TagHierarchyManager.Models;

partial class TagDatabase
{
    /// <summary>
    ///     Destructively wipes all the tags inside the database. Mainly used for debugging and testing.
    /// </summary>
    /// <returns>true if successful.</returns>
    public void ClearTags()
    {
        this.CheckInitialisation();
        using var transaction = this.currentConnection.BeginTransaction();
        var deleteCommand = this.currentConnection.CreateCommand();
        deleteCommand.CommandText = """
                                    -- noinspection SqlWithoutWhere
                                    DELETE FROM tag
                                    """;
        deleteCommand.Transaction = transaction;
        try
        {
            deleteCommand.ExecuteNonQuery();
            transaction.Commit();
            this.Tags.Clear();
        }
        catch (SqliteException)
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    ///     Deletes a <see cref="Tag" /> from the <see cref="TagDatabase" />.
    /// </summary>
    /// <param name="id">The ID of the tag to delete.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    public async Task DeleteTag(int id)
    {
        this.CheckInitialisation();
        var targetTag = await this.SelectTagFromDatabase(id);
        this.PerformDeletionChecks(targetTag);
        await this.ExecuteTagDeletion(targetTag!);
    }

    /// <summary>
    ///     Deletes a <see cref="Tag" /> from the <see cref="TagDatabase" />.
    /// </summary>
    /// <param name="name">The name of the tag to delete.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    private void DeleteFromCache(Tag targetTag)
    {
        Debug.WriteLine($"Deleting tag {targetTag.Name} ({targetTag.Id}) from cache.");
        this.Tags.Where(tag => tag.ParentIds.Contains(targetTag.Id))
            .ToList()
            .ForEach(tag =>
            {
                tag.ParentIds.Remove(targetTag.Id);
                tag.Parents.Remove(targetTag.Name);
            });

        var cachedTag = this.Tags.FirstOrDefault(t => t.Id == targetTag.Id);
        if (cachedTag != null) this.Tags.Remove(cachedTag);
    }

    private async Task ExecuteTagDeletion(Tag targetTag)
    {
        await using var transaction =
            (SqliteTransaction)await this.currentConnection!.BeginTransactionAsync().ConfigureAwait(false);
        var command = this.currentConnection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
                                DELETE FROM tag
                                WHERE id == @tag_id
                              """;
        command.Parameters.AddWithValue("@tag_id", targetTag.Id);
        try
        {
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            await transaction.CommitAsync().ConfigureAwait(false);
            this.DeleteFromCache(targetTag);
            this.TagsWritten?.Invoke(
                this, new DatabaseEditResult([], [], [(targetTag.Id, targetTag.Name)])
            );
        }
        catch (SqliteException)
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }

    private void PerformDeletionChecks(Tag? targetTag)
    {
        if (targetTag is null) throw new ArgumentException(ErrorMessages.TagDatabaseTagNotFound);

        if (this.GetTagChildren(targetTag.Id).Count > 0)
            throw new InvalidOperationException(ErrorMessages.TagDatabaseTagHasChildren);
    }
}