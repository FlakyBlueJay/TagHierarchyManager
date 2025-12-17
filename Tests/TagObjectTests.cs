using NUnit.Framework;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.Tests;

/// <summary>
///     Tests relating to the <see cref="Tag" /> object.
/// </summary>
public class TagObjectTests : TestBase
{
    private static IEnumerable<TestCaseData> ValidTagEntryTestCases
    {
        get
        {
            yield return new TestCaseData( // ambient - top level tag, no parents
                    "Ambient", // name
                    true, // is top level
                    ExpectedTagBindings, // tag bindings
                    new List<string>(), // parent names
                    string.Empty, // notes
                    new List<string>()) // aliases
                .SetName("TagEntry_NewTag_TopLevelNoParents");

            yield return new TestCaseData( // country - top level tag, with parents
                    "Country",
                    true,
                    ExpectedTagBindings,
                    new List<string> { "Northern American Music" },
                    "asdfghjkl",
                    new List<string> { "Country and Western" })
                .SetName("TagEntry_NewTag_TopLevelWIthParents");

            yield return new TestCaseData( // ambient americana - not top level, with parents
                    "Ambient Americana",
                    false,
                    ExpectedTagBindings,
                    new List<string> { "Ambient", "Northern American Music" },
                    "asdfghjkl",
                    new List<string> { "Ambient Country" })
                .SetName("TagEntry_NewTag_NotTopLevelNoParents");
        }
    }

    /// <summary>
    ///     Tests whether changing the top level allows a <see cref="Tag" /> with a parent can be validated successfully.
    /// </summary>
    /// <param name="value">The boolean value to change the test tag to.</param>
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void TagEntry_ChangeIsTopLevel_Validated(bool value)
    {
        // Arrange
        Tag testTag = new()
        {
            Name = "Ritual Ambient",
            Parents = ["Ambient"],
            IsTopLevel = false,
        };

        // Act
        testTag.IsTopLevel = value;

        // Assert
        Assert.That(testTag.Validate(), Is.EqualTo(true));
    }

    /// <summary>
    ///     Tests if a <see cref="Tag" /> object can be created and validated.
    /// </summary>
    /// <param name="name">The name of the tag.</param>
    /// <param name="isTopLevel">Whether the tag is top level.</param>
    /// <param name="tagBindings">The tag bindings associated with the tag.</param>
    /// <param name="parentNames">The names of the parents associated with the tag.</param>
    /// <param name="notes">The notes associated with the tag.</param>
    /// <param name="aliases">The aliases/A.K.A.s to associate with the tag.</param>
    [Test]
    [TestCaseSource(nameof(ValidTagEntryTestCases))]
    public void TagEntry_NewTag_Validated(
        string name, bool isTopLevel, List<string> tagBindings, List<string> parentNames, string notes,
        List<string> aliases)
    {
        // Arrange
        Tag testTag = new()
        {
            Name = name,
            IsTopLevel = isTopLevel,
            TagBindings = tagBindings,
            Parents = parentNames,
            Notes = notes,
            Aliases = aliases,
        };

        // Act
        bool tagValidated = testTag.Validate();

        // Assert
        Assert.That(tagValidated, Is.EqualTo(true));
    }

    /// <summary>
    ///     Tests if an <see cref="InvalidOperationException" /> when attempting to validate an orphan <see cref="Tag" /> (no
    ///     parents).
    /// </summary>
    [Test]
    public void TagEntry_OrphanTag_ThrowInvalidOperationExceptionOnOrphanTagCreation()
    {
        // Arrange
        Tag invalidTag = new()
        {
            Name = "Orphan Tag Test",
            IsTopLevel = false,
        };

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => invalidTag.Validate());
    }

    /// <summary>
    ///     Tests if an <see cref="InvalidOperationException" /> is thrown when attempting to validate an orphan
    ///     <see cref="Tag" /> with an empty list.
    /// </summary>
    [Test]
    public void TagEntry_OrphanTag_ThrowInvalidOperationExceptionOnOrphanTagWithEmptyList()
    {
        // Arrange
        Tag topLevelTag = new()
        {
            Name = "Ritual Ambient",
            Parents = [],
            IsTopLevel = false,
        };

        // Assert
        Assert.Throws<InvalidOperationException>(() => topLevelTag.Validate());
    }

    /// <summary>
    ///     Tests whether an <see cref="InvalidOperationException" /> is thrown when attempting to validate a tag whose
    ///     ParentNames list contains itself.
    /// </summary>
    [Test]
    public void TagEntry_SelfParentTag_ThrowInvalidOperationExceptionOnSelfParentAttempt()
    {
        // Arrange
        Tag topLevelTag = new()
        {
            Name = "Ambient",
            Parents = ["Ambient"],
            IsTopLevel = false,
        };

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => topLevelTag.Validate());
    }
}