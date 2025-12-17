using Microsoft.Data.Sqlite;
using NUnit.Framework;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.Tests;

/// <summary>
///     Tests relating to <see cref="TagDatabase" /> initialisation.
/// </summary>
[TestFixture]
public class TagDatabaseInitTests : TestBase
{
    private static readonly string TestDbsDir = Path.Combine(Path.GetTempPath(), "_taghierarchymanager_tests");

    /// <summary>
    ///     Sets up the temporary folder to store test files in.
    /// </summary>
    [OneTimeSetUp]
    public void SetUpTempFolder()
    {
        Directory.CreateDirectory(TestDbsDir);
    }

    /// <summary>
    ///     Tests if an initialisation error relating to the file not being a valid database are being sent.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task TagDatabaseInit_LoadTagDatabase_InitialisationErrorOnFileNotValidDatabase()
    {
        // Arrange
        const string invalidDbName = "invalid_file.thdb";
        string invalidDbPath = Path.Combine(TestDbsDir, invalidDbName);
        try
        {
            await using FileStream file = File.Open(invalidDbPath, FileMode.CreateNew);
        }
        catch (IOException)
        {
            // do nothing as the file exists already.
        }

        TagDatabase dummyDb = new();
        Exception? ex = Assert.ThrowsAsync<ArgumentException>(async () => await dummyDb.LoadAsync(invalidDbPath));

        // Act/Assert
        Assert.That(ex?.Message, Is.EqualTo(ErrorMessages.DbFileNotValid));
    }

    /// <summary>
    ///     Tests if initialisation errors relating to the file name are being sent correctly.
    /// </summary>
    /// <param name="fileName">The file name to test.</param>
    /// <param name="errorMessage">The expected error message.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    [TestCase("", ErrorMessages.FilePathIsEmpty)]
    [TestCase("invalid_file.exe", ErrorMessages.InvalidFileExtension)]
    public void TagDatabaseInit_NewTagDatabase_ErrorCaughtFileBased(string fileName, string errorMessage)
    {
        TagDatabase invalidDb = new();
        Exception? ex = Assert.ThrowsAsync<ArgumentException>(async () => await invalidDb.CreateAsync(fileName));

        Assert.That(ex?.Message, Is.EqualTo(errorMessage));
    }

    /// <summary>
    ///     Tests if an initialisation error relating to an invalid table structure is being sent.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task TagDatabaseInit_NewTagDatabase_InitialisationErrorOnInvalidTableStructure()
    {
        // Arrange
        SqliteConnection invalidConnection = new("Data Source=:memory:");
        await invalidConnection.OpenAsync();
        SqliteCommand command = invalidConnection.CreateCommand();
        command.CommandText = """
                              CREATE TABLE "invalid" (
                                  "id"    INTEGER
                              );
                              """;
        await command.ExecuteNonQueryAsync();

        Exception? ex =
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await this.Database.LoadAsync(connection: invalidConnection));

        // Act/Assert
        Assert.That(ex?.Message, Is.EqualTo(ErrorMessages.DbNotValid));
    }

    /// <summary>
    ///     Tests if an initialised database is created and initialised.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task TagDatabaseInit_NewTagDatabase_ReturnsInitializedDatabase()
    {
        // Arrange/Act
        bool isInitialised = false;
        TagDatabase db = new();
        db.InitialisationComplete += (_, _) => { isInitialised = true; };
        await db.CreateAsync(":memory:");

        // Assert
        Assert.That(db, Is.Not.Null);
        Assert.That(isInitialised, Is.True);
    }
}