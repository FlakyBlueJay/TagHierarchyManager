using Microsoft.Data.Sqlite;

namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    /// <summary>
    ///     Gets every tag inside the database, with an option to only grab top level tags.
    /// </summary>
    /// <param name="topLevelOnly">Whether to grab only top level tags from the database.</param>
    /// <param name="transaction">An optional SQLite transaction to run the command in.</param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation, returning a <see cref="List{Tag}" />
    ///     object containing all (or all top-level) <see cref="Tag" />s in the TagDatabase.
    /// </returns>
    public async Task<List<Tag>> GetAllTagsFromDatabase(bool topLevelOnly = false,
        SqliteTransaction? transaction = null)
    {
        this.CheckInitialisation();
        
        SqliteCommand command = this.currentConnection.CreateCommand();
        if (transaction is not null) command.Transaction = transaction;
        command.CommandText = """
                                  SELECT
                                      tag.id,
                                      tag.name,
                                      tag.top_level,
                                      tag.notes,
                                      tag.tags_to_bind,
                                      tag.also_known_as,
                                      GROUP_CONCAT(tag_parent_link.parent_tag_id, ';') AS parent_ids
                                  FROM tag
                                  LEFT JOIN tag_parent_link ON tag.id = tag_parent_link.target_tag_id
                                  GROUP BY tag.id
                              """;

        List<Tag> tags = await this.ExecuteTagRetrievalDatabaseQuery(command).ConfigureAwait(false);

        tags.ForEach(tag => tag.Parents = tags
            .Where(t => tag.ParentIds.Contains(t.Id))
            .Select(t => t.Name)
            .ToList());
        if (topLevelOnly)
            tags = tags.Where(tag => tag.IsTopLevel).ToList();
        return tags;
    }

    /// <summary>
    ///     Gets the children of a <see cref="Tag" />.
    /// </summary>
    /// <param name="id">an integer corresponding to a <see cref="Tag" />'s ID</param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation, returning a <see cref="List{Tag}" />
    ///     object containing all children of a particular <see cref="Tag" />.
    /// </returns>
    public List<Tag> GetTagChildren(int id)
    {
        List<Tag> tags = this.Tags.Where(tag => tag.ParentIds.Contains(id)).ToList();
        return tags;
    }

    /// <summary>
    ///     Gets the children of a <see cref="Tag" />.
    /// </summary>
    /// <param name="name">the string corresponding to a unique <see cref="Tag" /> name.</param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation, returning a <see cref="List{Tag}" />
    ///     object containing all children of a particular <see cref="Tag" />.
    /// </returns>
    public List<Tag> GetTagChildren(string name)
    {
        List<Tag> tags = this.Tags.Where(tag => tag.Parents.Contains(name)).ToList();
        return tags;
    }

    /// <summary>
    ///     Selects one specific tag by its exact name.
    /// </summary>
    /// <param name="name">The name of the tag.</param>
    /// <param name="transaction">An optional SQLite transaction to run the command in.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation, returning a Tag object or null.</returns>
    public async Task<Tag?> SelectTagFromDatabase(string name, SqliteTransaction? transaction = null)
    {
        this.CheckInitialisation();
        SqliteCommand command = this.currentConnection.CreateCommand();
        if (transaction is not null) command.Transaction = transaction;
        command.CommandText = """
                                SELECT
                                    tag.id,
                                    tag.name,
                                    tag.top_level,
                                    tag.notes,
                                    tag.tags_to_bind,
                                    tag.also_known_as,
                                    GROUP_CONCAT(tag_parent_link.parent_tag_id, ';') AS parent_ids
                                FROM tag
                                LEFT JOIN tag_parent_link ON tag.id = tag_parent_link.target_tag_id
                                WHERE tag.name = @tag_name
                                GROUP BY tag.id
                              """;
        command.Parameters.AddWithValue("@tag_name", name);
        List<Tag> tags = await this.ExecuteTagRetrievalDatabaseQuery(command).ConfigureAwait(false);
        Tag? selectedTag = tags.FirstOrDefault();
        if (selectedTag is null) return null;

        // Fetch parents within the same transaction if provided
        SqliteCommand parentCommand = this.currentConnection.CreateCommand();
        if (transaction is not null) parentCommand.Transaction = transaction;
        QueryProcessorHandler.ProcessTagParentSelectionCommand(parentCommand, selectedTag.Id);
        List<Tag> parents = await this.ExecuteTagRetrievalDatabaseQuery(parentCommand, false).ConfigureAwait(false);
        selectedTag.ParentIds = parents.Select(p => p.Id).ToList();
        selectedTag.Parents = parents.Select(p => p.Name).ToList();

        return selectedTag;
    }

    /// <summary>
    ///     Selects one specific tag by its exact ID in the database.
    /// </summary>
    /// <param name="id">The ID of the tag as it exists on the database.</param>
    /// <param name="transaction">An optional SQLite transaction to run the command in.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation, returning a Tag object or null.</returns>
    public async Task<Tag?> SelectTagFromDatabase(int id, SqliteTransaction? transaction = null)
    {
        this.CheckInitialisation();
        SqliteCommand command = this.currentConnection.CreateCommand();
        command.CommandText = """
                              SELECT
                                  tag.id,
                                  tag.name,
                                  tag.top_level,
                                  tag.notes,
                                  tag.tags_to_bind,
                                  tag.also_known_as,
                                  GROUP_CONCAT(tag_parent_link.parent_tag_id, ';') AS parent_ids
                              FROM tag
                              LEFT JOIN tag_parent_link ON tag.id = tag_parent_link.target_tag_id
                              WHERE tag.id = @tag_id
                              GROUP BY tag.id
                              """;
        command.Parameters.AddWithValue("@tag_id", id);
        List<Tag> tags = await this.ExecuteTagRetrievalDatabaseQuery(command).ConfigureAwait(false);
        Tag? selectedTag = tags.FirstOrDefault();
        if (selectedTag is null) return null;
        
        SqliteCommand parentCommand = this.currentConnection.CreateCommand();
        if (transaction is not null) parentCommand.Transaction = transaction;
        QueryProcessorHandler.ProcessTagParentSelectionCommand(parentCommand, selectedTag.Id);
        List<Tag> parents = await this.ExecuteTagRetrievalDatabaseQuery(parentCommand, false).ConfigureAwait(false);
        selectedTag.ParentIds = parents.Select(p => p.Id).ToList();
        selectedTag.Parents = parents.Select(p => p.Name).ToList();

        return selectedTag;
    }

    /// <summary>
    ///     Executes an SQLite command for retrieving tags and processing them into Tag objects.
    /// </summary>
    /// <param name="command">The SQLite command to process.</param>
    /// <param name="fetchParents">Whether the parents need to be fetched while running.</param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation,returning a <see cref="List{Tag}" />
    ///     containing <see cref="Tag" /> objects, contents varying on the query in <paramref name="command" />.
    /// </returns>
    private async Task<List<Tag>> ExecuteTagRetrievalDatabaseQuery(SqliteCommand command, bool fetchParents = true)
    {
        List<Tag> tags = [];

        try
        {
            await using SqliteDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            if (!reader.HasRows) return tags;
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                Tag addedTag = new()
                {
                    Id = reader.GetInt32(reader.GetOrdinal(IdColumnName)),
                    Name = reader.GetString(reader.GetOrdinal(NameColumnName)),
                    IsTopLevel = reader.GetBoolean(reader.GetOrdinal(TopLevelColumnName)),
                    Notes = reader.GetString(reader.GetOrdinal(NotesColumnName)),
                };

                string tagBindList = reader.GetString(reader.GetOrdinal(TagBindingsColumnName));
                if (!string.IsNullOrEmpty(tagBindList)) addedTag.TagBindings = tagBindList.Split(';').ToList();

                string altNameList = reader.GetString(reader.GetOrdinal(AliasesColumnName));
                if (!string.IsNullOrEmpty(altNameList)) addedTag.Aliases = altNameList.Split(';').ToList();


                if (fetchParents && !reader.IsDBNull(reader.GetOrdinal(ParentIdsColumnName)))
                {
                    List<int> parents =
                        reader.GetString(reader.GetOrdinal(ParentIdsColumnName)).Split(';').Select(int.Parse).ToList();
                    addedTag.ParentIds = parents;
                }

                tags.Add(addedTag);
            }

            return tags;
        }
        catch (SqliteException ex)
        {
            this.Logger.Error(ex, "An error occurred: {ErrorMessage} ", ex.Message);
            throw;
        }
    }
}