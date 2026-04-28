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
            await this.Transaction.RollbackAsync();
        }

        internal void AccumulateAdditions(Tag tag)
        {
            this._newlyAddedTags.Add(tag);
        }

        internal void AccumulateUpdates(Tag tag)
        {
            this._updatedTags.Add(tag);
        }
    }
}