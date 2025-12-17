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
        List<string> expectedNames = TestSampleTags.AllTags()
            .Select(tag => tag.Name)
            .ToList();

        // Act
        List<Tag> tags = await this.Database.GetAllTagsFromDatabase();
        List<string> retrievedNames = tags.Select(tag => tag.Name).ToList();

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
        List<string> expectedNames = TestSampleTags.AllTags()
            .Where(tag => tag.IsTopLevel)
            .Select(tag => tag.Name)
            .ToList();

        // Act
        List<Tag> tags = await this.Database.GetAllTagsFromDatabase(true);
        List<string> retrievedNames = tags.Select(tag => tag.Name).ToList();

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
        string tagNameToQuery = TestSampleTags.Ambient.Name;
        List<string> expectedNames =
        [
            TestSampleTags.DarkAmbient.Name,
            TestSampleTags.TribalAmbient.Name,
            TestSampleTags.SpaceAmbient.Name,
        ];

        // Act
        List<Tag> tags = this.Database.GetTagChildren(tagNameToQuery);
        List<string> retrievedNames = tags.Select(tag => tag.Name).ToList();

        // Assert
        Assert.That(tags.Count, Is.EqualTo(expectedNames.Count));
        Assert.That(retrievedNames, Is.EquivalentTo(expectedNames));
    }

    /// <summary>
    ///     Tests if SelectTag is selecting tags correctly.
    /// </summary>
    /// <param name="inputTag">The tag to select.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    [TestCaseSource(nameof(SelectTagTestCases))]
    public async Task TagDatabase_SelectTag(Tag inputTag)
    {
        // Act
        Tag? selectedTag = await this.Database.SelectTagFromDatabase(inputTag.Name);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(selectedTag!.Id, Is.Not.Null);
            Assert.That(selectedTag.ParentIds.Count, Is.EqualTo(inputTag.Parents.Count));
            Assert.That(selectedTag.Parents.Count, Is.EqualTo(inputTag.Parents.Count));
            Assert.That(selectedTag.Parents, Is.EqualTo(inputTag.Parents));
            Assert.That(selectedTag.Aliases.Count, Is.EqualTo(inputTag.Aliases.Count));
            Assert.That(selectedTag.Aliases, Is.EquivalentTo(inputTag.Aliases));
        }
    }

    /// <summary>
    ///     Tests if selecting a non-existent tag results in null.
    /// </summary>
    [Test]
    public void TagDatabase_SelectTag_ReturnNullOnTagNotFound()
    {
        const string nonexistentTagName = "This tag does not exist";
        Assert.ThatAsync(async () => await this.Database.SelectTagFromDatabase(nonexistentTagName), Is.Null);
    }
}