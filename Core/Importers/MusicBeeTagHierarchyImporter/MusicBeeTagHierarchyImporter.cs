using TagHierarchyManager.Common;
using TagHierarchyManager.Models;

namespace TagHierarchyManager.Importers;
/// <summary>
///     Implements an importer for converting a MusicBee tag hierarchy template to a Dictionary of <see cref="ImportedTag"/>s.
/// </summary>
public partial class MusicBeeTagHierarchyImporter : Importer
{
    private const char CommentSymbol = ';';
    private const int IndentSize = 4; // MusicBee is strict about having an indent size of 4 spaces.
    private const string TagBindingSeparator = "::";

    private static readonly string[] CommentSymbols = [";", "//"];
    
    /// <summary>
    ///     Initializes a new instance of the <see cref="MusicBeeTagHierarchyImporter" /> class.
    /// </summary>
    public MusicBeeTagHierarchyImporter()
    {
        this.FormatName = FileTypes.MusicBeeTagHierarchyTemplate.Name;
    }

    /// <summary>
    ///     Converts a MusicBee tag hierarchy template into a Dictionary of <see cref="ImportedTag"/>s.
    /// </summary>
    /// <param name="importedData">The tag hierarchy data string.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation, returning a Dictionary of <see cref="ImportedTag"/>s.</returns>
    protected override async Task<Dictionary<string, ImportedTag>> ProcessDataToDatabaseAsync(string importedData)
    {
        importedData = importedData.TrimEnd();
        
        ValidateHierarchyData(importedData);
        
        Dictionary<string, ImportedTag> tagsToImport = new();
        
        int previousIndentLevel = 0;
        List<string> parentStack = [];
        int lineCounter = 1;

        string lastTagName = string.Empty;
        using StringReader reader = new(importedData);
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            if (IsEmptyOrComment(line)) continue;

            TagHierarchyLine parsedLine = new(line, lineCounter);
            UpdateParentStack(parentStack, parsedLine, previousIndentLevel, lastTagName);
            
            ImportTag(tagsToImport, parsedLine, parentStack);

            lastTagName = parsedLine.TagName;
            previousIndentLevel = parsedLine.IndentLevel;
            lineCounter++;
        }

        return tagsToImport;
    }

    private static void AddTagBindingIfMissing(ImportedTag currentTag, string tagBinding)
    {
        if (!string.IsNullOrEmpty(tagBinding))
            currentTag.TagBindings.Add(tagBinding);
    }

    private static bool IsEmptyOrComment(string line)
    {
        if (CommentSymbols.Any(symbol => line.StartsWith(symbol, StringComparison.Ordinal)))
            return true;

        return string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line);
    }

    private static void UpdateParentStack(List<string> parentStack, TagHierarchyLine currentLine, int previousIndent,
        string parentName)
    {
        if (currentLine.IndentLevel > previousIndent)
        {
            if (currentLine.IndentLevel - previousIndent > 1)
                throw new ArgumentException(
                    ErrorMessages.TagHierarchyIndentIsExcessive(currentLine.LineNumber));

            if (!string.IsNullOrEmpty(parentName)) parentStack.Add(parentName);
        }
        else if (currentLine.IndentLevel < previousIndent)
        {
            if (currentLine.IndentLevel <= parentStack.Count)
                parentStack.RemoveRange(currentLine.IndentLevel, parentStack.Count - currentLine.IndentLevel);
            else
                throw new InvalidOperationException(ErrorMessages.TagHierarchyPopAttemptOutOfRange);
        }
    }

    private static void ValidateHierarchyData(string tagHierarchyData)
    {
        if (tagHierarchyData.Contains('\t')) throw new ArgumentException(ErrorMessages.TagHierarchyTabsDetected);

        if (tagHierarchyData.StartsWith(' ')) throw new ArgumentException(ErrorMessages.TagHierarchyStartsWithSpace);
    }

    private static void ImportTag(Dictionary<string, ImportedTag> importDict, TagHierarchyLine line, List<string> parentStack)
    {
        ImportedTag? existingTag = importDict.GetValueOrDefault(line.TagName);
        if (existingTag is null)
        {
            ImportedTag newTag = new()
            {
                Name = line.TagName,
                IsTopLevel = parentStack.Count <= 0,
            };
            importDict[line.TagName] = newTag;
        }
        
        ProcessParents(importDict[line.TagName], parentStack);
        AddTagBindingIfMissing(importDict[line.TagName], line.Binding);
    }
    
    
    private static void ProcessParents(ImportedTag tag, List<string> parentStack)
    {
        if (parentStack.Count == 0)
        {
            tag.IsTopLevel = true;
            return;
        }

        string parentName = parentStack[^1];
        if (tag.Name != parentName)
        {
            tag.Parents.Add(parentName);
        }
    }

    private struct TagHierarchyLine
    {
        public readonly string Binding = string.Empty;
        public readonly int IndentLevel = 0;
        public readonly int LineNumber = 0;
        public readonly string TagName;

        public TagHierarchyLine(string line, int lineCounter)
        {
            this.LineNumber = lineCounter;
            string trimmedLine = line.TrimStart();

            int separatorIndex = trimmedLine.LastIndexOf(TagBindingSeparator, StringComparison.Ordinal);
            if (separatorIndex != -1)
            {
                this.TagName = trimmedLine[..separatorIndex];
                this.Binding = trimmedLine[(separatorIndex + TagBindingSeparator.Length)..];
            }
            else
            {
                this.TagName = trimmedLine;
            }

            int indentRemainder = (line.Length - trimmedLine.Length) % IndentSize;
            if (indentRemainder != 0)
                throw new ArgumentException(
                    ErrorMessages.TagHierarchyIndentIsUneven(lineCounter));

            this.IndentLevel = (line.Length - trimmedLine.Length) / IndentSize;
        }
    }
}