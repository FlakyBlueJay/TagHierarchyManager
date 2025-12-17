using NUnit.Framework;
using TagHierarchyManager.Exporters;

namespace TagHierarchyManager.Tests;

/// <summary>
///     Tests relating to exporter classes.
/// </summary>
[TestFixture]
public class ExporterTests : TestBase
{
    /// <summary>
    ///     Tests to see if the test database exports to a MusicBee tag hierarchy template correctly.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public void ExportAsync_ExportToMusicBeeTagHierarchy_ExportedStringMatches()
    {
        // Arrange
        const string expectedExport = """
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
                                      """;

        // Act
        string exportedTagHierarchy = new MusicBeeTagHierarchyExporter().ExportDatabase(this.Database);
        exportedTagHierarchy = exportedTagHierarchy.TrimEnd();

        // Assert
        Assert.That(exportedTagHierarchy, Is.EqualTo(expectedExport));
    }
}