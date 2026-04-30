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
        var sampleOfSampleData = new List<Tag>()
        {
            TestSampleTags.Ambient, TestSampleTags.Electronic
        };
            
        this.Database.ClearTags();
        foreach (var tag in sampleOfSampleData)
            await this.Database.WriteTagToDatabase(tag);
    }
    
    [Test]
    public async Task TagDatabase_WriteTagsToDatabase_TagDeleted()
    {
        // Arrange
        TagDatabase.DatabaseEditResult? test = null;
        TagDatabase db = new();
        await db.CreateAsync(":memory:");
        db.InitialisationComplete += (_, _) => Assert.Pass();
        EventHandler<TagDatabase.DatabaseEditResult> handler = (_, result) => test = result;
        db.TagsWritten += handler;
        Tag deletedTag = new()
        {
            Name = "DELETE ME",
            IsTopLevel = true,
        };
        await db.WriteTagToDatabase(deletedTag);

        // Act
        await db.DeleteTag(deletedTag.Id);
        
        // Assert
        Assert.That(test!.Added.Count, Is.EqualTo(0));
        Assert.That(test.Updated.Count, Is.EqualTo(0));
        Assert.That(test.Deleted.Count, Is.EqualTo(1));
        Assert.That(db.Tags.Any(t => t.Name == deletedTag.Name), Is.False);
        
        db.TagsWritten -= handler;
    }

    /// <summary>
    ///     Tests if an <see cref="ArgumentException" /> is thrown on deleting a tag that does not exist.
    /// </summary>
    [Test]
    public void TagDatabase_DeleteTag_ThrowExceptionOnDeletingNonExistentTag()
    {
        ArgumentException? exId =
            Assert.ThrowsAsync<ArgumentException>(async () => await this.Database.DeleteTag(1000));
        Assert.That(exId?.Message, Is.EqualTo(ErrorMessages.TagNotFound));
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
        var testTags = new List<Tag> {firstParentTag, secondParentTag, childTag};

        foreach (var tag in testTags)
        {
            await this.Database.WriteTagToDatabase(tag);
        }
        
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
        foreach (var parentTag in childTag.Parents.Select(parent => this.Database.Tags.First(p => p.Name == parent)))
        {
            childTag.ParentIds.Add(parentTag.Id);
        }
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
        foreach (var parentTag in inputTag.Parents.Select(parent => this.Database.Tags.First(p => p.Name == parent)))
        {
            inputTag.ParentIds.Add(parentTag.Id);
        }

        // Act
        await this.Database.WriteTagToDatabase(inputTag);
        var savedTag = this.Database.Tags.First(t => t.Name == inputTag.Name);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tagValidated, Is.True);
            Assert.That(inputTag.Id, Is.GreaterThan(0));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(savedTag.Parents.Count, Is.EqualTo(inputTag.Parents.Count));
            Assert.That(savedTag.Parents, Is.EquivalentTo(inputTag.Parents));
            Assert.That(savedTag.ParentIds.Count, Is.EqualTo(inputTag.Parents.Count));
            List<string> parentTags = await savedTag.ParentIds.ToAsyncEnumerable()
                .Select(async (int parentId, CancellationToken _) =>
                {
                    Tag? tag = await this.Database.SelectTagFromDatabase(parentId);
                    return tag!.Name;
                }).ToListAsync();
            Assert.That(parentTags, Is.EquivalentTo(inputTag.Parents));
            Assert.That(savedTag.Aliases.Count, Is.EqualTo(inputTag.Aliases.Count));
            Assert.That(savedTag.Aliases, Is.EquivalentTo(inputTag.Aliases));
        }
    }

    [Test]
    public async Task TagDatabase_WriteTagsToDatabase_TagAdded()
    {
        // Arrange
        List<Tag> testTags = [TestSampleTags.Ambient, TestSampleTags.Electronic, TestSampleTags.SpaceAmbient];
        this.Database.ClearTags();
        
        // Act
        foreach (var tag in testTags)
        {
            await this.Database.WriteTagToDatabase(tag);
        }

        // Assert
        var retrievedTags = await this.Database.GetAllTagsFromDatabase();
        Assert.That(retrievedTags.Count, Is.EqualTo(testTags.Count));
    }

    [Test]
    public async Task TagDatabase_WriteTagsToDatabase_TagWrittenAddedEvent()
    {
        // Arrange
        TagDatabase.DatabaseEditResult? test = null;
        TagDatabase db = new();
        await db.CreateAsync(":memory:");
        db.InitialisationComplete += (_, _) => Assert.Pass();
        db.TagsWritten += (_, result) => test = result;
        
        // Act
        await db.WriteTagToDatabase(TestSampleTags.Ambient);
        
        // Assert
        Assert.That(test!.Added.Count, Is.EqualTo(1));
        Assert.That(test.Updated.Count, Is.EqualTo(0));
        Assert.That(test.Deleted.Count, Is.EqualTo(0));
    }
    
    [Test]
    public async Task TagDatabase_WriteTagsToDatabase_TagWrittenUpdatedEvent()
    {
        // Arrange
        TagDatabase.DatabaseEditResult? test = null;
        TagDatabase db = new();
        await db.CreateAsync(":memory:");
        db.InitialisationComplete += (_, _) => Assert.Pass();
        EventHandler<TagDatabase.DatabaseEditResult> handler = (_, result) => test = result;
        db.TagsWritten += handler;
        
        // Act
        await db.WriteTagToDatabase(TestSampleTags.Ambient);
        var retrievedTag = db.Tags.First(t => t.Name == TestSampleTags.Ambient.Name);
        retrievedTag.Notes = "test edit";
        await db.WriteTagToDatabase(retrievedTag);
        Assert.That(test!.Updated.Count, Is.EqualTo(1));
        
        // Assert
        Assert.That(test.Added.Count, Is.EqualTo(0));
        Assert.That(test.Updated.Count, Is.EqualTo(1));
        Assert.That(test.Deleted.Count, Is.EqualTo(0));
        
        db.TagsWritten -= handler;
    }

    [Test]
    public async Task TagDatabase_WriteTagsToDatabase_TagWrittenEvent_ExternalTransaction()
    {
        // Arrange
        TagDatabase.DatabaseEditResult? test = null;
        TagDatabase db = new();
        await db.CreateAsync(":memory:");
        db.InitialisationComplete += (_, _) => Assert.Pass();

        EventHandler<TagDatabase.DatabaseEditResult> handler = (_, result) => test = result;
        db.TagsWritten += handler;
        
        List<Tag> testTags = [TestSampleTags.Ambient, TestSampleTags.Electronic, TestSampleTags.SpaceAmbient];
        
        // Act/Assert
        var transaction = await db.BeginExternalTransactionAsync();
        foreach (var tag in testTags)
        {
           await db.WriteTagToDatabase(tag, transaction);
        }
        await transaction.CommitAsync();
        
        Assert.That(test!.Added.Count, Is.EqualTo(testTags.Count));
        Assert.That(test.Updated.Count, Is.EqualTo(0));
        Assert.That(test.Deleted.Count, Is.EqualTo(0));
        
        Tag retrievedTag = db.Tags.First(t => t.Name == testTags[0].Name);
        Assert.That(retrievedTag, Is.Not.Null);
        retrievedTag.Notes = "test edit";
        await db.WriteTagToDatabase(retrievedTag);
        Assert.That(test.Updated.Count, Is.EqualTo(1));
        
        Tag deletedTag = db.Tags.First(t => t.Name == testTags[2].Name);
        await db.DeleteTag(deletedTag.Id);
        Assert.That(test.Deleted.Count, Is.EqualTo(1));
        Assert.That(db.Tags.FirstOrDefault(t => t.Name == testTags[2].Name), Is.Null);
        
        db.TagsWritten -= handler;
    }
}
    