using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    /// <summary>
    ///     A class for handling settings for a <see cref="TagDatabase" /> at a lower level.
    /// </summary>
    /// <param name="db">The <see cref="TagDatabase" /> that owns the handler.</param>
    public sealed class SettingsHandler(TagDatabase db)
    {
        /// <summary>
        ///     The setting key for whichever tag binding should be default, if a tag binding doesn't get passed when adding to the
        ///     database.
        /// </summary>
        internal const string DefaultTagBindingKey = "default_tag_bind";

        private const string SettingKeyParameter = "@setting_key";
        private const string SettingValueParameter = "@setting_value";
        private const string VersionKey = "version";

        private readonly Dictionary<string, string> defaultSettings = new()
        {
            { DefaultTagBindingKey, "genre" },
        };

        private static IReadOnlyList<string> RequiredSettingsKeys { get; } =
            [VersionKey, DefaultTagBindingKey];

        /// <summary>
        ///     Creates setting "key" with the given value.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value associated with this key.</param>
        /// <returns>true if the creation is successful.</returns>
        /// <exception cref="ArgumentException">Thrown if the key already exists.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the database has not been properly initialised.</exception>
        public async Task CreateSettingAsync(string key, string value)
        {
            db.CheckInitialisation();

            if (await this.CheckSettingExistenceAsync(key).ConfigureAwait(false))
                throw new ArgumentException(ErrorMessages.SettingKeyAlreadyExists(key));
            SqliteCommand insertCommand = db.Connection.CreateCommand();

            insertCommand.CommandText = $"""
                                             INSERT INTO settings (key, value)
                                             VALUES({SettingKeyParameter}, {SettingValueParameter});
                                         """;
            insertCommand.Parameters.AddWithValue(SettingKeyParameter, key);
            insertCommand.Parameters.AddWithValue(SettingValueParameter, value);
            await insertCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            db.Logger.Debug("Setting \"{@Key}\" with value \"{@Value}\" successfully added.", key, value);
        }

        /// <summary>
        ///     Deletes a specific setting.
        /// </summary>
        /// <param name="key">The requested setting key.</param>
        /// <returns>True if deletion was successful.</returns>
        /// <exception cref="InvalidOperationException">Thrown if an attempt to delete a required setting was made.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if the key was not found.</exception>
        public async Task DeleteSettingAsync(string key)
        {
            db.CheckInitialisation();

            if (RequiredSettingsKeys.Contains(key))
                throw new InvalidOperationException(ErrorMessages.SettingIsRequired);

            if (!await this.CheckSettingExistenceAsync(key).ConfigureAwait(false))
                throw new KeyNotFoundException(ErrorMessages.SettingKeyNotFound(key));
            SqliteCommand command = db.Connection.CreateCommand();
            command.CommandText = $"""
                                       DELETE FROM settings
                                       WHERE key == {SettingKeyParameter}
                                   """;
            command.Parameters.AddWithValue(SettingKeyParameter, key);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///     Grabs all settings from the settings table.
        /// </summary>
        /// <exception cref="InvalidOperationException">Called if the tag hierarchy database has not been initialised, yet.</exception>
        /// <returns>A <see cref="Dictionary{TKey, TValue}" /> with strings for keys and values, wrapped in a <see cref="Task" />.</returns>
        public async Task<Dictionary<string, string>> GetAllSettingsAsync()
        {
            db.CheckInitialisation();
            Dictionary<string, string> settingsDict = new();
            SqliteCommand command = db.Connection.CreateCommand();
            command.CommandText = "SELECT * FROM settings";
            await using SqliteDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                string key = reader.GetString(0);
                string value = reader.GetString(1);
                settingsDict.Add(key, value);
            }

            return settingsDict;
        }

        /// <summary>
        ///     Grab a specific setting's value from the settings table.
        /// </summary>
        /// <param name="key">The requested setting key.</param>
        /// <returns>The value of the setting.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the key was not found.</exception>
        public async Task<string?> GetSettingValueAsync(string key)
        {
            db.CheckInitialisation();

            if (!await this.CheckSettingExistenceAsync(key).ConfigureAwait(false))
                throw new KeyNotFoundException(ErrorMessages.SettingKeyNotFound(key));
            SqliteCommand command = db.Connection.CreateCommand();
            command.CommandText = $"""
                                       SELECT value FROM settings
                                       WHERE key == {SettingKeyParameter}
                                   """;
            command.Parameters.AddWithValue(SettingKeyParameter, key);
            string? pokedSetting = (string?)await command.ExecuteScalarAsync().ConfigureAwait(false);
            return pokedSetting;
        }

        /// <summary>
        ///     Sets the settings back to default based on the <see cref="defaultSettings" /> dictionary in the handler.
        /// </summary>
        public void ResetDefaultSettings()
        {
            db.DefaultTagBindings = this.defaultSettings[DefaultTagBindingKey]
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        /// <summary>
        ///     Updates a specific setting's value.
        /// </summary>
        /// <param name="key">The requested setting key.</param>
        /// <param name="value">The new value for the setting.</param>
        /// <returns>true if the setting change was successful.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the key was not found.</exception>
        public async Task<bool> UpdateSettingAsync(string key, string value)
        {
            db.CheckInitialisation();

            if (!await this.CheckSettingExistenceAsync(key).ConfigureAwait(false))
                throw new KeyNotFoundException(ErrorMessages.SettingKeyNotFound(key));
            SqliteCommand command = db.Connection.CreateCommand();
            command.CommandText = $"""
                                       UPDATE settings
                                       SET value = {SettingValueParameter}
                                       WHERE key = {SettingKeyParameter};
                                   """;
            command.Parameters.AddWithValue(SettingKeyParameter, key);
            command.Parameters.AddWithValue(SettingValueParameter,
                key == DefaultTagBindingKey ? string.Join(';', value) : value);

            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            return true;
        }

        private async Task<bool> CheckSettingExistenceAsync(string key)
        {
            db.CheckInitialisation();

            SqliteCommand command = db.Connection.CreateCommand();
            command.CommandText = $"""
                                       SELECT COUNT(*) FROM settings
                                       WHERE key = {SettingKeyParameter};
                                   """;
            command.Parameters.AddWithValue(SettingKeyParameter, key);
            int count = Convert.ToInt32(await command.ExecuteScalarAsync().ConfigureAwait(false),
                CultureInfo.InvariantCulture);
            return count > 0;
        }
    }
}