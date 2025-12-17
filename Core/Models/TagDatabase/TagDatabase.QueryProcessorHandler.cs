using Microsoft.Data.Sqlite;

namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    /// <summary>
    ///     A class for handling the processing of queries to be sent to the <see cref="TagDatabase" />.
    /// </summary>
    internal static class QueryProcessorHandler
    {
        private const string TagParentSelectionBaseCommand = """
                                                                 SELECT
                                                                     parent_tag_id AS id,
                                                                     parent.name,
                                                                     parent.top_level,
                                                                     parent.notes,
                                                                     parent.tags_to_bind,
                                                                     parent.also_known_as
                                                                 FROM tag_parent_link
                                                                 LEFT JOIN tag parent
                                                                 ON tag_parent_link.parent_tag_id = parent.id
                                                                 WHERE target_tag_id == @target_id
                                                             """;

        /// <summary>
        ///     Builds an SQLite SELECT command to retrieve the parents of a Tag.
        /// </summary>
        /// <param name="command">the SQLiteCommand object to add onto.</param>
        /// <param name="id">an integer corresponding to a Tag ID</param>
        internal static void ProcessTagParentSelectionCommand(SqliteCommand command, int id)
        {
            ProcessTagParentSelectionInt(command, id);
        }
        
        /// <summary>
        ///     Generates an SQLITE INSERT command for saving the tag to the database.
        /// </summary>
        /// <param name="command">the SQLiteCommand object to add the generated command to.</param>
        /// <param name="tag">the Tag object to process.</param>
        internal static void ProcessTagSaveCommand(SqliteCommand command, Tag tag)
        {
            if (tag.Id == 0)
            {
                // if tag.Id is null, it's assumed it's a new tag to be added.
                command.CommandText = """
                                          INSERT INTO tag (name, notes, top_level, tags_to_bind, also_known_as)
                                          VALUES (@name, @notes, @is_top_level, @tags_to_bind, @aliases)
                                          RETURNING id;
                                      """;
            }
            else
            {
                // ...otherwise, we know that this has been saved into the database.
                command.CommandText = """
                                          UPDATE tag
                                          SET
                                              name = @name,
                                              notes = @notes,
                                              top_level = @is_top_level,
                                              tags_to_bind = @tags_to_bind,
                                              also_known_as = @aliases
                                          WHERE id = @target_id
                                          RETURNING id;
                                      """;
                command.Parameters.AddWithValue("@target_id", tag.Id);
            }

            command.Parameters.AddWithValue("@name", tag.Name);
            command.Parameters.AddWithValue("@notes", tag.Notes);
            command.Parameters.AddWithValue("@is_top_level", tag.IsTopLevel ? 1 : 0);
            command.Parameters.AddWithValue("@tags_to_bind", string.Join(';', tag.TagBindings));
            command.Parameters.AddWithValue("@aliases", string.Join(';', tag.Aliases));
        }

        private static void ProcessTagParentSelectionInt(SqliteCommand command, int id)
        {
            if (id == 0)
                throw new ArgumentException(ErrorMessages.EmptyParameters);

            command.CommandText = TagParentSelectionBaseCommand;
            command.Parameters.AddWithValue("@target_id", id);
        }
    }
}