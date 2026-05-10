using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    /// <summary>
    ///     Saves a Tag object to the database.
    /// </summary>
    /// <param name="tag">The List of Tag objects.</param>
    /// <param name="transaction">The SqliteTransaction to execute queries on, will make its own if null.</param>
    /// <exception cref="ArgumentException">Thrown if the tag already existed in the database.</exception>
    public async Task WriteTagToDatabase(Tag tag, SqliteTransaction? transaction = null)
    {
        this.CheckInitialisation();
        tag.Validate();
        var isTransactionOwner = transaction == null;
        transaction ??= (SqliteTransaction)await this._currentConnection.BeginTransactionAsync().ConfigureAwait(false);

        var oldTag = new Tag
        {
            Id = tag.Id,
            Name = tag.Name,
            ParentIds = [..tag.ParentIds],
            TagBindings = [..tag.TagBindings],
            Aliases = [..tag.Aliases],
            Notes = tag.Notes,
            IsTopLevel = tag.IsTopLevel
        };

        try
        {
            var addCommand = this._currentConnection.CreateCommand();
            addCommand.Transaction = transaction;
            QueryProcessorHandler.ProcessTagSaveCommand(addCommand, tag);

            tag.Id = Convert.ToInt32(await addCommand.ExecuteScalarAsync().ConfigureAwait(false),
                CultureInfo.InvariantCulture);
            await this.SaveTagParentIds(transaction, tag);

            var index = this.Tags.FindIndex(t => t.Id == tag.Id);

            tag.CreatedAt ??= DateTime.Now;
            tag.UpdatedAt = DateTime.Now;

            if (isTransactionOwner) await transaction.CommitAsync();
            if (oldTag.Id != 0)
            {
                this.Tags[index] = tag;
                this.TagsWritten?.Invoke(this, new DatabaseEditResult([], [tag], []));
            }
            else
            {
                this.Tags.Add(tag);
                this.TagsWritten?.Invoke(this, new DatabaseEditResult([tag], [], []));
            }
        }
        catch (Exception)
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            if (oldTag.Id == 0)
            {
                tag.Id = 0;
                throw;
            }

            var index = this.Tags.FindIndex(t => t.Id == tag.Id);
            this.Tags[index] = oldTag;
            throw;
        }
        finally
        {
            if (isTransactionOwner) await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task WriteTagToDatabase(Tag tag, ExternalTransaction transaction)
    {
        this.CheckInitialisation();
        tag.Validate();

        var oldTag = new Tag
        {
            Id = tag.Id,
            Name = tag.Name,
            ParentIds = [..tag.ParentIds],
            TagBindings = [..tag.TagBindings],
            Aliases = [..tag.Aliases],
            Notes = tag.Notes,
            IsTopLevel = tag.IsTopLevel
        };

        try
        {
            var addCommand = this._currentConnection.CreateCommand();
            addCommand.Transaction = transaction.Transaction;
            QueryProcessorHandler.ProcessTagSaveCommand(addCommand, tag);

            tag.Id = Convert.ToInt32(await addCommand.ExecuteScalarAsync().ConfigureAwait(false),
                CultureInfo.InvariantCulture);
            await this.SaveTagParentIds(transaction.Transaction, tag);

            tag.CreatedAt ??= DateTime.Now;
            tag.UpdatedAt = DateTime.Now;

            if (oldTag.Id != 0)
                transaction.AccumulateUpdates(tag);
            else transaction.AccumulateAdditions(tag);
        }
        catch (Exception)
        {
            if (oldTag.Id == 0)
            {
                tag.Id = 0;
                throw;
            }

            var index = this.Tags.FindIndex(t => t.Id == tag.Id);
            this.Tags[index] = oldTag;
            throw;
        }
    }


    private async Task SaveTagParentIds(SqliteTransaction transaction, Tag tag)
    {
        if (tag.ParentIds.Count == 0) return;
        this.CheckInitialisation();

        // clear existing tag parents so we have a clean slate.
        var deleteCommand = this._currentConnection.CreateCommand();
        deleteCommand.Transaction = transaction;
        deleteCommand.CommandText = """
                                        DELETE FROM tag_parent_link
                                        WHERE target_tag_id = @tag_id
                                    """;
        deleteCommand.Parameters.AddWithValue("@tag_id", tag.Id);
        await deleteCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        // process + add parent links
        var parentCommand = this._currentConnection.CreateCommand();
        parentCommand.Transaction = transaction;
        parentCommand.CommandText = """
                                        INSERT INTO tag_parent_link (target_tag_id, parent_tag_id)
                                        VALUES (@target_tag_id, @parent_tag_id)
                                    """;
        parentCommand.Parameters.Clear();
        parentCommand.Parameters.AddWithValue("@target_tag_id", tag.Id);
        parentCommand.Parameters.Add("@parent_tag_id", SqliteType.Integer);
        await parentCommand.PrepareAsync();

        foreach (var parentId in tag.ParentIds)
        {
            parentCommand.Parameters["@parent_tag_id"].Value = (long)parentId;
            await parentCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }
}