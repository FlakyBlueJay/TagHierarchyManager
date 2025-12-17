using NUnit.Framework;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.Tests;

/// <summary>
///     Tests for using the <see cref="TagDatabase.SettingsHandler" /> at a lower level.
/// </summary>
[TestFixture]
public class SettingsTests : TestBase
{
    private const string AddedTestKey = "testkey";
    private const string AddedTestValue = "testvalue";
    private const string NonExistentKey = "testkeydoesntexist";

    /// <summary>
    ///     Tests if a setting can be successfully created.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task CreateSetting_SuccessfulSettingCreation()
    {
        // Arrange/Act
        await this.Database.Settings.CreateSettingAsync(AddedTestKey, AddedTestValue);
        string? retrievedSetting = await this.Database.Settings.GetSettingValueAsync(AddedTestKey);

        // Assert
        Assert.That(retrievedSetting, Is.EqualTo(AddedTestValue));
    }

    /// <summary>
    ///     Checks if adding a setting that already exists results in an ArgumentException being thrown.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task CreateSetting_ThrowArgumentExceptionIfAlreadyExists()
    {
        // Arrange
        await this.AddTestSetting();

        // Act/Assert
        ArgumentException ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await this.Database.Settings.CreateSettingAsync(AddedTestKey, AddedTestValue)) !;
        Assert.That(ex.Message, Is.EqualTo(ErrorMessages.SettingKeyAlreadyExists(AddedTestKey)));
    }

    /// <summary>
    ///     DefaultTagBindings has a setter that automatically updates on the database when it itself is updated. This tests if
    ///     the setting successfully updates.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task DbProperties_SetViaProperty_ChangedDefaultTagBindings()
    {
        // Arrange
        List<string> changedTagBindings = ["genre", "album genre"];

        // Act
        this.Database.DefaultTagBindings = changedTagBindings;

        // Assert
        string? settingValueString = await this.Database.Settings.GetSettingValueAsync(ExpectedTagBindKey);
        List<string> changedTagBindingsList = settingValueString!
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        Assert.That(settingValueString, Is.EqualTo(string.Join(';', this.Database.DefaultTagBindings)));
        Assert.That(changedTagBindingsList, Is.EquivalentTo(changedTagBindings));
    }

    /// <summary>
    ///     Tests if the custom test setting can be deleted.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task DeleteSetting_CustomSettingDeleted()
    {
        // Arrange
        await this.AddTestSetting();

        // Act
        await this.Database.Settings.DeleteSettingAsync(AddedTestKey);

        // Assert
        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await this.Database.Settings.GetSettingValueAsync(AddedTestKey));
    }

    /// <summary>
    ///     Tests if, when attempting to delete a setting whose key is part of the required setting keys, an
    ///     <see cref="InvalidOperationException" /> is thrown.
    /// </summary>
    /// <param name="key">The key to evaluate.</param>
    [Test]
    [TestCase(ExpectedVersionKey)]
    [TestCase(ExpectedTagBindKey)]
    public void DeleteSetting_ThrowInvalidOperationExceptionIfSettingIsRequired(string key)
    {
        Assert.ThrowsAsync<InvalidOperationException>(async () => await this.Database.Settings.DeleteSettingAsync(key));
    }

    /// <summary>
    ///     Tests if, when attempting to delete a setting with a key that doesn't exist, a KeyNotFoundException is thrown.
    /// </summary>
    [Test]
    public void DeleteSetting_ThrowKeyNotFoundExceptionIfSettingNotFound()
    {
        KeyNotFoundException ex = Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await this.Database.Settings.DeleteSettingAsync(NonExistentKey)) !;
        Assert.That(ex.Message, Is.EqualTo(ErrorMessages.SettingKeyNotFound(NonExistentKey)));
    }

    /// <summary>
    ///     Tests if the required setting keys exist.
    /// </summary>
    /// <param name="key">The key to validate.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    [TestCase(ExpectedVersionKey)]
    [TestCase(ExpectedTagBindKey)]
    public async Task GetAllSettings_RequiredSettingsKeysValidated(string key)
    {
        // Arrange/Act
        Dictionary<string, string> retrievedKeys = await this.Database.Settings.GetAllSettingsAsync();

        // Assert
        Assert.That(retrievedKeys.Keys, Contains.Item(key));
    }

    /// <summary>
    ///     Checks if the custom test setting is present when using GetAllSettingAsync.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task GetAllSettings_WithCustomSetting_CustomSettingInAllSettings()
    {
        // Arrange
        await this.AddTestSetting();

        // Act
        Dictionary<string, string> retrievedKeys = await this.Database.Settings.GetAllSettingsAsync();

        // Assert
        Assert.That(retrievedKeys.Keys, Contains.Item(AddedTestKey));
        Assert.That(retrievedKeys[AddedTestKey], Is.EqualTo(AddedTestValue));
    }

    /// <summary>
    ///     Sets the settings back to default on every set up.
    /// </summary>
    [SetUp]
    public void ResetDefaultSettings()
    {
        this.Database.Settings.ResetDefaultSettings();
    }

    /// <summary>
    ///     Checks if the custom test setting can be updated.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task UpdateSetting_SettingUpdated()
    {
        // Arrange
        await this.AddTestSetting();
        const string expectedUpdatedValue = "22222";

        // Act
        await this.Database.Settings.UpdateSettingAsync(AddedTestKey, expectedUpdatedValue);

        // Assert
        await Assert.ThatAsync(async () => await this.Database.Settings.GetSettingValueAsync(AddedTestKey),
            Is.EqualTo(expectedUpdatedValue));
    }

    /// <summary>
    ///     Tests if, when trying to find a setting key that does not exist in the <see cref="TagDatabase" />'s settings, a
    ///     KeyNotFoundException is thrown.
    /// </summary>
    [Test]
    public void UpdateSetting_ThrowKeyNotFoundExceptionIfSettingNotFound()
    {
        KeyNotFoundException? ex = Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await this.Database.Settings.UpdateSettingAsync(NonExistentKey, "testvalue"));
        Assert.That(ex!.Message, Is.EqualTo(ErrorMessages.SettingKeyNotFound(NonExistentKey)));
    }

    /// <summary>
    ///     Adds the test setting if it doesn't exist.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    private async Task AddTestSetting()
    {
        try
        {
            await this.Database.Settings.GetSettingValueAsync(AddedTestKey);
        }
        catch (KeyNotFoundException)
        {
            await this.Database.Settings.CreateSettingAsync(AddedTestKey, AddedTestValue);
        }
    }
}