using NUnit.Framework;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.Tests;

/// <summary>
///     The base class for every test for the application.
/// </summary>
public abstract class TestBase
{
    /// <summary>
    ///     The expected version value in the database.
    /// </summary>
    public const int ExpectedVersion = 1;

    /// <summary>
    ///     The expected default tag binding key in the database.
    /// </summary>
    protected const string ExpectedTagBindKey = "default_tag_bind";

    /// <summary>
    ///     The expected version key in the database.
    /// </summary>
    protected const string ExpectedVersionKey = "version";

    protected static readonly List<string> ExpectedTagBindings = ["genre", "style"];

    /// <summary>
    ///     Gets the database to set up for the tests.
    /// </summary>
    protected readonly TagDatabase Database = new();

    /// <summary>
    ///     A logger that writes to the console, using the Serilog library.
    /// </summary>
    private static readonly ILogger Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console(theme: AnsiConsoleTheme.Code, applyThemeToRedirectedOutput: true)
        .CreateLogger();

    /// <summary>
    ///     Adds sample data to the database, with exceptions for tests that do not/cannot use the test data.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    [SetUp]
    public async Task PopulateSampleData()
    {
        // classes and tests that don't interface with the sample data
        // and therefore do not need this to execute.
        HashSet<string> ignoredClasses =
        [
            nameof(TagDatabaseInitTests),
            nameof(SettingsTests),
            nameof(TagObjectTests),
            nameof(TagDatabaseWriteTests),
            nameof(ImporterTests),
        ];

        string? className = TestContext.CurrentContext.Test.ClassName;
        Logger.Debug("[TestBase.PopulateSampleData] Current class name: {ClassName}", className);
        if (className == null || ignoredClasses.Contains(className.Replace("TagHierarchyManager.Tests.", string.Empty)))
        {
            Logger.Debug("[TestBase.PopulateSampleData] Ignoring as class name {ClassName} in ignoredClasses", className);
            return;
        }

        List<Tag> sampleTags = TestSampleTags.AllTags();
        this.Database.ClearTags();
        foreach (Tag inputTag in sampleTags) await this.Database.WriteTagToDatabase(inputTag);
    }

    // do not use in Test1_Init. That needs to be tested at a lower level.

    /// <summary>
    ///     Creates the <see cref="TagDatabase" /> in memory and configures the DefaultTagBindings to the expected ones for the
    ///     tests.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    [OneTimeSetUp]
    public async Task SetUpDatabaseForTesting()
    {
        await this.Database.CreateAsync(":memory:");
        this.Database.DefaultTagBindings = ExpectedTagBindings;
    }

    [OneTimeTearDown]
    private void ExitDatabase()
    {
        this.Database.Close();
    }

    // resharper disable MemberCanBePrivate.Global
    /// <summary>
    ///     Stores the sample tag data.
    /// </summary>
    protected static class TestSampleTags
    {
        /// <summary>
        ///     Gets the Ambient test tag.
        /// </summary>
        public static Tag Ambient => new()
        {
            Name = "Ambient",
            IsTopLevel = true,
            TagBindings = ExpectedTagBindings,
        };

        /// <summary>
        ///     Gets the Dark Ambient test tag (parents: Ambient, Post-Industrial).
        /// </summary>
        public static Tag DarkAmbient => new()
        {
            Name = "Dark Ambient",
            IsTopLevel = false,
            TagBindings = ExpectedTagBindings,
            Parents = ["Ambient", "Post-Industrial"],
            Aliases = ["Ambient Industrial"],
        };

        /// <summary>
        ///     Gets the Electronic test tag.
        /// </summary>
        public static Tag Electronic => new()
        {
            Name = "Electronic",
            IsTopLevel = true,
            TagBindings = ExpectedTagBindings,
        };

        /// <summary>
        ///     Gets the Industrial &amp; Noise test tag.
        /// </summary>
        public static Tag IndustrialAndNoise => new()
        {
            Name = "Industrial & Noise",
            IsTopLevel = true,
            TagBindings = ExpectedTagBindings,
        };

        /// <summary>
        ///     Gets the Post-Industrial test tag (parents: Industrial &amp; Noise).
        /// </summary>
        public static Tag PostIndustrial => new()
        {
            Name = "Post-Industrial",
            IsTopLevel = false,
            TagBindings = ExpectedTagBindings,
            Parents = ["Industrial & Noise"],
        };

        /// <summary>
        ///     Gets the Ritual Ambient test tag (parents: Dark Ambient).
        /// </summary>
        public static Tag RitualAmbient => new()
        {
            Name = "Ritual Ambient",
            IsTopLevel = false,
            TagBindings = ExpectedTagBindings,
            Parents = ["Dark Ambient"],
            Aliases = ["Ritual Dark Ambient", "Dark Ritual Ambient"],
        };

        /// <summary>
        ///     Gets the Space Ambient test tag (parents: Ambient, Electronic).
        /// </summary>
        public static Tag SpaceAmbient => new()
        {
            Name = "Space Ambient",
            IsTopLevel = false,
            TagBindings = ExpectedTagBindings,
            Parents = ["Ambient", "Electronic"],
        };

        /// <summary>
        ///     Gets the Tribal Ambient test tag (parents: Tribal Ambient).
        /// </summary>
        public static Tag TribalAmbient => new()
        {
            Name = "Tribal Ambient",
            IsTopLevel = false,
            TagBindings = ExpectedTagBindings,
            Parents = ["Ambient"],
            Aliases = ["Ethnic Ambient", "Ethno Ambient"],
        };

        /// <summary>
        ///     Gets all tags in the SampleTags class.
        /// </summary>
        /// <returns>a <see cref="List{Tag}" /> of <see cref="Tag" />s with the sample tag data.</returns>
        public static List<Tag> AllTags()
        {
            return
            [
                Ambient,
                Electronic,
                IndustrialAndNoise,
                PostIndustrial,
                DarkAmbient,
                RitualAmbient,
                SpaceAmbient,
                TribalAmbient,
            ];
        }
    }
}