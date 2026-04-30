using Microsoft.Data.Sqlite;

namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    /// <summary>
    ///     An object representing an SQLite transaction started outside of the confines of the TagDatabase object (e.g. in the
    ///     Tag Hierarchy Manager UI).
    /// </summary>
    public class ExternalTransaction(TagDatabase db, SqliteTransaction transaction) : IAsyncDisposable
    {
        internal readonly SqliteTransaction Transaction = transaction;

        private readonly List<Tag> _newlyAddedTags = [];
        private readonly List<Tag> _updatedTags = [];
        private readonly List<Tag> _updatedTagSnapshots = [];

        public async Task CommitAsync()
        {
            await this.Transaction.CommitAsync();

            foreach (var tag in this._newlyAddedTags) db.Tags.Add(tag);

            foreach (var tag in this._updatedTags)
            {
                var index = db.Tags.FindIndex(t => t.Id == tag.Id);
                if (index != -1)
                    db.Tags[index] = tag;
            }

            db.TagsWritten?.Invoke(db, new DatabaseEditResult(this._newlyAddedTags, this._updatedTags, []));
        }

        public async ValueTask DisposeAsync()
        {
            await this.Transaction.DisposeAsync();
        }

        public async Task RollbackAsync()
        {
            foreach (var tag in this._newlyAddedTags) tag.Id = 0;

            foreach (var tag in this._updatedTagSnapshots)
            {
                var index = db.Tags.FindIndex(t => t.Id == tag.Id);
                if (index != -1)
                    db.Tags[index] = tag;
            }

            await this.Transaction.RollbackAsync();
        }

        internal void AccumulateAdditions(Tag tag)
        {
            this._newlyAddedTags.Add(tag);
        }

        internal void AccumulateUpdates(Tag tag)
        {
            this._updatedTagSnapshots.Add(new Tag
            {
                Id = tag.Id,
                Name = tag.Name,
                Parents = [..tag.Parents],
                ParentIds = [..tag.ParentIds],
                TagBindings = [..tag.TagBindings],
                Aliases = [..tag.Aliases],
                Notes = tag.Notes,
                IsTopLevel = tag.IsTopLevel,
                CreatedAt = tag.CreatedAt,
                UpdatedAt = tag.UpdatedAt
            });
            this._updatedTags.Add(tag);
        }
    }
}