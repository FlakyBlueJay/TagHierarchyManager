using NUnit.Framework;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.Tests;

/// <summary>
///     Tests relating to saving of <see cref="Tag" /> objects to a <see cref="TagDatabase" />.
/// </summary>
[TestFixture]
public class TagDatabaseWriteTests : TestBase
{
    private static IEnumerable<TestCaseData> WriteTagTestCases
    {
        get
        {
            yield return new TestCaseData(TestSampleTags.SpaceAmbient);
            yield return new TestCaseData(TestSampleTags.TribalAmbient);
        }
    }

    /// <summary>
    ///     Clears the database and adds some sample data.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [SetUp]
    public async Task ClearDatabaseAndAddSampleData()
    {
        this.Database.ClearTags();
        await this.Database.WriteTagToDatabase(TestSampleTags.Ambient);
        await this.Database.WriteTagToDatabase(TestSampleTags.Electronic);
    }

    /// <summary>
    ///     Tests if deletion of a tag works, by checking if the result of trying to select that tag's name returns null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task TagDatabase_DeleteTag_TagIsDeleted()
    {
        // Arrange
        Tag deletedTag = new()
        {
            Name = "DELETE ME",
            IsTopLevel = true,
        };
        await this.Database.WriteTagToDatabase(deletedTag);

        // Act
        await this.Database.DeleteTag(deletedTag.Id);

        // Assert
        await Assert.ThatAsync(async () => await this.Database.SelectTagFromDatabase(deletedTag.Name), Is.Null);
    }

    /// <summary>
    ///     Tests if an <see cref="ArgumentException" /> is thrown on deleting a tag that does not exist.
    /// </summary>
    [Test]
    public void TagDatabase_DeleteTag_ThrowExceptionOnDeletingNonExistentTag()
    {
        using (Assert.EnterMultipleScope())
        {
            ArgumentException? exName = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this.Database.DeleteTag("This tag does not exist"));
            ArgumentException? exId =
                Assert.ThrowsAsync<ArgumentException>(async () => await this.Database.DeleteTag(1000));
            Assert.That(exName?.Message, Is.EqualTo(ErrorMessages.TagNotFound));
            Assert.That(exId?.Message, Is.EqualTo(ErrorMessages.TagNotFound));
        }
    }

    /// <summary>
    ///     Tests if a tag is being edited and saved to the <see cref="TagDatabase" /> successfully.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public async Task TagDatabase_WriteTagToDatabase_EditTag()
    {
        // Arrange
        Tag firstParentTag = new()
        {
            Name = "Test parent tag 1",
            IsTopLevel = true,
        };
        Tag secondParentTag = new()
        {
            Name = "Test parent tag 2",
            IsTopLevel = true,
        };
        Tag childTag = new()
        {
            Name = "Test child tag",
            IsTopLevel = false,
            TagBindings = ["genre"],
            Parents = ["Test parent tag 1"],
        };
        await this.Database.WriteTagToDatabase(firstParentTag);
        await this.Database.WriteTagToDatabase(secondParentTag);
        await this.Database.WriteTagToDatabase(childTag);
        int childTagId = childTag.Id;
        List<int> expectedParents = [firstParentTag.Id, secondParentTag.Id];

        const string newName = "Test child tag (edited)";
        const bool newTopLevel = true;
        const string addedTagBind = "style";
        const string addedParentName = "Test parent tag 2";
        const string newNotes = "Test note edit";
        const string newAlias = "Test child tag (alias added)";
        List<string> newAliases = [newAlias];

        // Act
        childTag.Name = newName;
        childTag.IsTopLevel = newTopLevel;
        childTag.TagBindings.Add(addedTagBind);
        childTag.Parents.Add(addedParentName);
        childTag.Notes = newNotes;
        childTag.Aliases = newAliases;
        await this.Database.WriteTagToDatabase(childTag);

        // Assert
        Tag? editedChildTag = await this.Database.SelectTagFromDatabase(childTagId);
        const int expectedParentCount = 2;
        const int expectedAliasCount = 1;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editedChildTag!.Id, Is.GreaterThan(0));
            Assert.That(editedChildTag.ParentIds.Count, Is.EqualTo(expectedParentCount));
            Assert.That(editedChildTag.ParentIds, Is.EquivalentTo(expectedParents));
            Assert.That(editedChildTag.Aliases.Count, Is.EqualTo(expectedAliasCount));
            Assert.That(editedChildTag.Aliases, Does.Contain(newAlias));
        }
    }

    /// <summary>
    ///     Tests if an attempt to save a tag that already exists results in an <see cref="ArgumentException" />.
    /// </summary>
    [Test]
    public void TagDatabase_WriteTagToDatabase_ThrowExceptionOnTagAlreadyExists()
    {
        // Arrange
        Tag ambient = TestSampleTags.Ambient;

        // Act/Assert
        ArgumentException? ex =
            Assert.ThrowsAsync<ArgumentException>(async () => await this.Database.WriteTagToDatabase(ambient));
        Assert.That(ex!.Message, Does.EndWith("already exists in the database."));
    }

    /// <summary>
    ///     Tests if tags are being saved to and retrieved from the <see cref="TagDatabase" /> successfully.
    /// </summary>
    /// <param name="inputTag">The tag to save.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    [TestCaseSource(nameof(WriteTagTestCases))]
    public async Task TagDatabase_WriteTagToDatabase_WriteTag(Tag inputTag)
    {
        // Arrange
        bool tagValidated = inputTag.Validate();

        // Act
        await this.Database.WriteTagToDatabase(inputTag);
        Tag? savedTag = await this.Database.SelectTagFromDatabase(inputTag.Name);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tagValidated, Is.True);
            Assert.That(inputTag.Id, Is.GreaterThan(0));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(savedTag!.Parents.Count, Is.EqualTo(inputTag.Parents.Count));
            Assert.That(savedTag.Parents, Is.EquivalentTo(inputTag.Parents));
            Assert.That(savedTag.ParentIds.Count, Is.EqualTo(inputTag.Parents.Count));
            List<string> parentTags = await savedTag.ParentIds.ToAsyncEnumerable()
                .SelectAwait(async parentId =>
                {
                    Tag? tag = await this.Database.SelectTagFromDatabase(parentId);
                    return tag!.Name;
                }).ToListAsync();
            Assert.That(parentTags, Is.EquivalentTo(inputTag.Parents));
            Assert.That(savedTag.Aliases.Count, Is.EqualTo(inputTag.Aliases.Count));
            Assert.That(savedTag.Aliases, Is.EquivalentTo(inputTag.Aliases));
        }
    }
}