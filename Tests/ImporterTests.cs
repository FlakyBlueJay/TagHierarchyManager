using NUnit.Framework;
using TagHierarchyManager.Exporters;
using TagHierarchyManager.Importers;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.Tests;

/// <summary>
///     Tests relating to importer classes.
/// </summary>
[TestFixture]
public class ImporterTests : TestBase
{
    /// <summary>
    ///     Tests if, when importing a MusicBee tag hierarchy template, it is exported back out to the same output.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task ImportAsync_ImportMusicBeeTagHierarchy_ExportedStringMatches()
    {
        const string expectedExport = """
                                      Genres
                                          Ambient
                                              Ambient::genre
                                              Ambient::style
                                              Dark Ambient
                                                  Dark Ambient::genre
                                                  Dark Ambient::style
                                                  Ritual Ambient::genre
                                                  Ritual Ambient::style
                                              Space Ambient::genre
                                              Space Ambient::style
                                              Tribal Ambient::genre
                                              Tribal Ambient::style
                                          Electronic
                                              Electronic::genre
                                              Electronic::style
                                              Space Ambient::genre
                                              Space Ambient::style
                                          Industrial & Noise
                                              Industrial & Noise::genre
                                              Industrial & Noise::style
                                              Post-Industrial
                                                  Post-Industrial::genre
                                                  Post-Industrial::style
                                                  Dark Ambient
                                                      Dark Ambient::genre
                                                      Dark Ambient::style
                                                      Ritual Ambient::genre
                                                      Ritual Ambient::style
                                      Scenes & Movements
                                          Demoscene::movement
                                      """;
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, expectedExport);


        // Act
        TagDatabase db = new();
        Dictionary<string, ImportedTag> testData =
            await new MusicBeeTagHierarchyImporter().ImportFromFileAsync(tempFilePath);
        await db.CreateAsync(":memory:", tagsToImport: testData);

        string exportedTagHierarchy = new MusicBeeTagHierarchyExporter().ExportDatabase(db);
        exportedTagHierarchy = exportedTagHierarchy.TrimEnd();

        // Assert
        Assert.That(exportedTagHierarchy, Is.EqualTo(expectedExport.TrimEnd()));
    }

    // the below should be unit tests really, but I'll do that when I've actually wrapped my head around mocks and stuff.
    // I'd rather reliable integration tests than unreliable, crappy unit tests.
    //
    // Also, the below test disables StyleCop's SA1027 (Tabs and spaces must be used correctly)
    // as they are deliberately malformed for testing purposes.

    /// <summary>
    ///     Tests for if <see cref="ArgumentException" />s are thrown when attempting to import a tag hierarchy with tabs or
    ///     that starts with a space.
    /// </summary>
    /// <param name="brokenHierarchy">The string to attempt to send to the importer.</param>
    /// <param name="exceptionMessage">The expected error message.</param>
    [Test]
    [TestCase(
#pragma warning disable SA1027
        """
        Ambient
        	Ambient::genre
        	Ambient::style
        """, MusicBeeTagHierarchyImporter.ErrorMessages.TagHierarchyTabsDetected)]
    [TestCase(" Ambient", MusicBeeTagHierarchyImporter.ErrorMessages.TagHierarchyStartsWithSpace)]
    public async Task ImportAsync_ImportMusicBeeTagHierarchy_ArgumentExceptionThrown(string brokenHierarchy,
        string exceptionMessage)
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, brokenHierarchy);

        Importer importer = new MusicBeeTagHierarchyImporter();
        Exception? ex =
            Assert.ThrowsAsync<ArgumentException>(async () => await importer.ImportFromFileAsync(tempFilePath));
        Assert.That(ex!.Message, Is.EqualTo(exceptionMessage));
    }

    /// <summary>
    ///     Tests for if <see cref="ArgumentException" />s are thrown on particular edge cases relating to the spacing of tags
    ///     in a tag hierarchy template.<br />
    ///     These are for exceptions where a line number is expected to be included in the error message.
    /// </summary>
    /// <param name="brokenHierarchy">The string to attempt to send to the importer.</param>
    /// <param name="lineNumber">The line number to be expected.</param>
    /// <param name="exceptionMessage">The expected error message template.</param>
    [Test]
    [TestCase(
        """
        Ambient
           Ambient::genre
           Ambient::style
        """, 2, MusicBeeTagHierarchyImporter.ErrorMessages.IndentIsUnevenTemplate)]
    [TestCase(
        """
        Ambient
                Ambient::genre
                Ambient::style
        """, 2, MusicBeeTagHierarchyImporter.ErrorMessages.IndentIsExcessiveTemplate)]
    public async Task ImportAsync_ImportMusicBeeTagHierarchy_TagHierarchyDataExceptionThrown(string brokenHierarchy,
        int lineNumber, string exceptionMessage)
    {
        // Arrange
        string expectedExceptionMessage = string.Format(exceptionMessage, lineNumber);
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, brokenHierarchy);
        Importer importer = new MusicBeeTagHierarchyImporter();
        Exception? ex =
            Assert.ThrowsAsync<ArgumentException>(async () => await importer.ImportFromFileAsync(tempFilePath));
        Assert.That(ex!.Message, Is.EqualTo(expectedExceptionMessage));
    }
}