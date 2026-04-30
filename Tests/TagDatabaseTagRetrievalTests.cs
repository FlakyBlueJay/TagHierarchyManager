using NUnit.Framework;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.Tests;

/// <summary>
///     Tests relating to the retrieval of tag from a <see cref="TagDatabase" />.
/// </summary>
[TestFixture]
public class TagDatabaseTagRetrievalTests : TestBase
{
    private static IEnumerable<TestCaseData> SelectTagTestCases
    {
        get
        {
            yield return new TestCaseData(TestSampleTags.Ambient);
            yield return new TestCaseData(TestSampleTags.SpaceAmbient);
            yield return new TestCaseData(TestSampleTags.RitualAmbient);
        }
    }

    /// <summary>
    ///     Tests if retrieving all tags from the test <see cref="TagDatabase" /> matches
    ///     <see cref="TestBase.TestSampleTags.AllTags" />.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task TagDatabase_GetAllTags_AllTags()
    {
        // Arrange
        var expectedNames = TestSampleTags.AllTags()
            .Select(tag => tag.Name)
            .ToList();

        // Act
        var tags = await this.Database.GetAllTagsFromDatabase();
        var retrievedNames = tags.Select(tag => tag.Name).ToList();

        // Assert
        Assert.That(tags.Count, Is.EqualTo(expectedNames.Count));
        Assert.That(retrievedNames, Is.EquivalentTo(expectedNames));
    }

    /// <summary>
    ///     Tests if retrieving all top-level tags from the test <see cref="TagDatabase" /> matches
    ///     <see cref="TestBase.TestSampleTags.AllTags" /> (filtered only tags where IsTopLevel is true)
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task TagDatabase_GetAllTags_AllTopLevelTags()
    {
        // Arrange
        var expectedNames = TestSampleTags.AllTags()
            .Where(tag => tag.IsTopLevel)
            .Select(tag => tag.Name)
            .ToList();

        // Act
        var tags = await this.Database.GetAllTagsFromDatabase(true);
        var retrievedNames = tags.Select(tag => tag.Name).ToList();

        // Assert
        Assert.That(tags.Count, Is.EqualTo(expectedNames.Count));
        Assert.That(retrievedNames, Is.EquivalentTo(expectedNames));
    }

    /// <summary>
    ///     Tests if GetTagChildren retrieves all expected tags.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public void TagDatabase_GetTagChildren()
    {
        // Arrange
        var tagNameToQuery = TestSampleTags.Ambient.Name;
        List<string> expectedNames =
        [
            TestSampleTags.DarkAmbient.Name,
            TestSampleTags.TribalAmbient.Name,
            TestSampleTags.SpaceAmbient.Name
        ];

        // Act
        var tags = this.Database.GetTagChildren(tagNameToQuery);
        var retrievedNames = tags.Select(tag => tag.Name).ToList();

        // Assert
        Assert.That(tags.Count, Is.EqualTo(expectedNames.Count));
        Assert.That(retrievedNames, Is.EquivalentTo(expectedNames));
    }
}