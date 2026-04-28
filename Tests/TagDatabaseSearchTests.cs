using NUnit.Framework;
using TagHierarchyManager.Common;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.Tests;

/// <summary>
///     Tests relating to search functionality.
/// </summary>
[TestFixture]
public class TagDatabaseSearchTests : TestBase
{
    private const string TestQuery = "ambient";

    private readonly Dictionary<string, List<string>> expectedSearchResults = new()
    {
        {
            "NoAKAsFuzzy",
            TestSampleTags.AllTags()
                .Where(tag => tag.Name.Contains(TestQuery, StringComparison.CurrentCultureIgnoreCase))
                .Select(tag => tag.Name)
                .ToList()
        },
        {
            "NoAKAsStartsWith",
            TestSampleTags.AllTags()
                .Where(tag => tag.Name.StartsWith(TestQuery, StringComparison.CurrentCultureIgnoreCase))
                .Select(tag => tag.Name)
                .ToList()
        },
        {
            "NoAKAsEndsWith",
            TestSampleTags.AllTags()
                .Where(tag => tag.Name.ToLower().EndsWith(TestQuery, StringComparison.CurrentCultureIgnoreCase))
                .Select(tag => tag.Name)
                .ToList()
        },
        {
            "NoAKAsExactMatch",
            TestSampleTags.AllTags()
                .Where(tag => tag.Name.ToLower() == TestQuery.ToLower())
                .Select(tag => tag.Name)
                .ToList()
        },
        {
            "WithAKAsFuzzy",
            TestSampleTags.AllTags()
                .Where(tag =>
                    tag.Aliases.Any(alias => alias.ToLower().Contains(TestQuery, StringComparison.OrdinalIgnoreCase)) ||
                    tag.Name.ToLower().Contains(TestQuery))
                .Select(tag => tag.Name)
                .ToList()
        },
        {
            "WithAKAsStartsWith",
            TestSampleTags.AllTags()
                .Where(tag =>
                    tag.Aliases.Any(alias => alias.StartsWith(TestQuery, StringComparison.OrdinalIgnoreCase)) ||
                    tag.Name.ToLower().StartsWith(TestQuery, StringComparison.OrdinalIgnoreCase))
                .Select(tag => tag.Name)
                .ToList()
        },
        {
            "WithAKAsEndsWith",
            TestSampleTags.AllTags()
                .Where(tag => tag.Aliases.Any(alias =>
                                  alias.EndsWith(TestQuery, StringComparison.OrdinalIgnoreCase)) ||
                              tag.Name.ToLower().EndsWith(TestQuery, StringComparison.OrdinalIgnoreCase))
                .Select(tag => tag.Name)
                .ToList()
        },
        {
            "WithAKAsExactMatch",
            TestSampleTags.AllTags().Where(tag => tag.Aliases.Any(alias => alias.ToLower() == TestQuery.ToLower()) ||
                                                  tag.Name.ToLower() == TestQuery.ToLower())
                .Select(tag => tag.Name)
                .ToList()
        },
    };

    /// <summary>
    ///     Tests the lack of results using a query known to not exist in the test data, checking if the tag count is equal to
    ///     zero.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    public void TagDatabase_SearchForTags_NoResults()
    {
        // Arrange
        // ReSharper disable once StringLiteralTypo - intentionally using a typo'd version of Ambient.
        const string query = "ambionte";

        // Act
        List<Tag> tags = this.Database.Search(query, TagDatabaseSearchMode.Fuzzy);
        List<Tag> tagsWithAliases = this.Database.SearchWithAliases(query, TagDatabaseSearchMode.Fuzzy);
        // Assert
        Assert.That(tags.Count, Is.EqualTo(0));
        Assert.That(tagsWithAliases.Count, Is.EqualTo(0));
    }

    /// <summary>
    ///     Tests that a search can be made and data can be retrieved with a <see cref="Tag" /> name and query with diacritics.
    /// </summary>
    /// <param name="query">The query to test.</param>
    /// <param name="mode">The search mode to use, should be selected using the <see cref="TagDatabaseSearchMode" /> enum.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]
    [TestCase("áéíóúçýỷủ", TagDatabaseSearchMode.Fuzzy, TestName = "TagDatabase_SearchForTags_Normalised_Fuzzy")]
    [TestCase("Tag test áéíóúç", TagDatabaseSearchMode.StartsWith,
        TestName = "TagDatabase_SearchForTags_Normalised_StartsWith")]
    [TestCase("áéíóúçýỷủ", TagDatabaseSearchMode.EndsWith, TestName = "TagDatabase_SearchForTags_Normalised_EndsWith")]
    [TestCase("Tag test áéíóúçýỷủ", TagDatabaseSearchMode.ExactMatch,
        TestName = "TagDatabase_SearchForTags_Normalised_ExactMatch")]
    [TestCase("áéíóúçýỷủ", TagDatabaseSearchMode.ExactMatch,
        TestName = "TagDatabase_SearchForTags_Normalised_ExactMatch")]
    public async Task TagDatabase_SearchForTags_NormalisedDiacritics(string query, TagDatabaseSearchMode mode)
    {
        // Arrange
        Tag normalisedTest = new()
        {
            Name = "Tag test áéíóúçýỷủ",
            IsTopLevel = false,
            TagBindings = ["genre", "style"],
            Parents = ["Ambient", "Electronic"],
            Aliases = ["áéíóúçýỷủ"],
        };
        await this.Database.WriteTagToDatabase(normalisedTest);

        // Act
        List<Tag> tags = this.Database.SearchWithAliases(query, mode);
        List<string> tagNames = tags.Select(tag => tag.Name).ToList();

        // Assert
        Assert.That(tags.Count, Is.EqualTo(1));
        Assert.That(
            tagNames.All(name => name.Contains(query, StringComparison.OrdinalIgnoreCase)),
            $"This tag did not contain the search query `{query}`");
    }

    /// <summary>
    ///     Tests whether the search functionality is functioning correctly using "ambient" as the query.
    /// </summary>
    /// <param name="mode">The search mode to use, should be selected using the <see cref="TagDatabaseSearchMode" /> enum.</param>
    /// <param name="expectedResultKey">
    ///     The key in the <see cref="expectedSearchResults" /> that stores the List compare the
    ///     retrieved tag names to.
    /// </param>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Test]

    // ambient, dark ambient, tribal ambient, ritual ambient, space ambient
    [TestCase(TagDatabaseSearchMode.Fuzzy, "NoAKAsFuzzy",
        TestName = "TagDatabase_SearchForTags_WithResults_NoAKAsFuzzy")]

    // ambient
    [TestCase(TagDatabaseSearchMode.StartsWith, "NoAKAsStartsWith",
        TestName = "TagDatabase_SearchForTags_WithResults_NoAKAsStartsWith")]

    // ambient, dark ambient, tribal ambient, ritual ambient, space ambient
    [TestCase(TagDatabaseSearchMode.EndsWith, "NoAKAsEndsWith",
        TestName = "TagDatabase_SearchForTags_WithResults_NoAKAsEndsWith")]

    // ambient
    [TestCase(TagDatabaseSearchMode.ExactMatch, "NoAKAsExactMatch",
        TestName = "TagDatabase_SearchForTags_WithResults_NoAKAsExactMatch")]
    public void TagDatabase_SearchForTags_SearchResultsNoAlias(TagDatabaseSearchMode mode, string expectedResultKey)
    {
        List<Tag> retrievedTags = this.Database.Search(TestQuery, mode);

        List<string> retrievedTagNames = retrievedTags.Select(tag => tag.Name).ToList();

        Assert.That(retrievedTagNames, Is.EquivalentTo(this.expectedSearchResults[expectedResultKey]));
    }


    // ambient, dark ambient (AMBIENT industrial), tribal ambient, ritual ambient, space ambient
    [TestCase(TagDatabaseSearchMode.Fuzzy, "WithAKAsFuzzy",
        TestName = "TagDatabase_SearchForTags_WithResults_WithAKAsFuzzy")]

    // ambient, ambient, dark ambient (AMBIENT industrial)
    [TestCase(TagDatabaseSearchMode.StartsWith, "WithAKAsStartsWith",
        TestName = "TagDatabase_SearchForTags_WithResults_WithAKAsStartsWith")]

    // ambient, dark ambient, tribal ambient (ethnic AMBIENT), ritual ambient (ritual dark AMBIENT, dark ritual AMBIENT), space ambient
    [TestCase(TagDatabaseSearchMode.EndsWith, "WithAKAsEndsWith",
        TestName = "TagDatabase_SearchForTags_WithResults_WithAKAsEndsWith")]

    // ambient
    [TestCase(TagDatabaseSearchMode.ExactMatch, "WithAKAsExactMatch",
        TestName = "TagDatabase_SearchForTags_WithResults_WithAKAsExactMatch")]
    public void TagDatabase_SearchForTags_SearchResultsWithAlias(TagDatabaseSearchMode mode, string expectedResultKey)
    {
        List<Tag> retrievedTags = this.Database.SearchWithAliases(TestQuery, mode);

        List<string> retrievedTagNames = retrievedTags.Select(tag => tag.Name).ToList();

        Assert.That(retrievedTagNames, Is.EquivalentTo(this.expectedSearchResults[expectedResultKey]));
    }
}