namespace TagHierarchyManager.Importers;

public partial class MusicBeeTagHierarchyImporter
{
    /// <summary>
    ///     A class storing error messages for <see cref="MusicBeeTagHierarchyImporter" /> exceptions.
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>
        ///     Indicates that an excessive amount of indents was detected in the tag hierarchy template, with a
        ///     placeholder for the line number.
        /// </summary>
        public const string IndentIsExcessiveTemplate =
            "Excessive indent was detected at line {0}.";

        /// <summary>
        ///     Indicates that an uneven amount of indents was detected in the tag hierarchy template, with a
        ///     placeholder for the line number.
        /// </summary>
        public const string IndentIsUnevenTemplate =
            "Uneven indent was found at line {0}.";

        /// <summary>
        ///     Indicates an attempt to remove a parent tag from the parent stack resulting in an Exception.
        /// </summary>
        public const string TagHierarchyPopAttemptOutOfRange =
            "Process exited abruptly due to an error with handling the indent. (current line's indent level was greater than the amount of tags in the parent stack)";

        /// <summary>
        ///     Indicates that a space character was detected at the beginning of the tag hierarchy template.
        /// </summary>
        public const string TagHierarchyStartsWithSpace =
            "Tag hierarchy starts with a space, which is not valid for a tag hierarchy template's structure.";

        /// <summary>
        ///     Indicates that a tab character was detected in tag hierarchy template.
        /// </summary>
        public const string TagHierarchyTabsDetected =
            "Tab characters were detected, which is an invalid structure for MusicBee tag hierarchy templates.";

        /// <summary>
        ///     Constructs a string indicating an excessive amount of indents in the tag hierarchy template.
        /// </summary>
        /// <param name="lineNumber">The offending line number of the template.</param>
        /// <returns>A string stating that an excessive amount of indents was found at a particular given line.</returns>
        public static string TagHierarchyIndentIsExcessive(int lineNumber)
        {
            return string.Format(IndentIsExcessiveTemplate, lineNumber);
        }

        /// <summary>
        ///     Constructs a string indicating an uneven indent in the tag hierarchy template.
        /// </summary>
        /// <param name="lineNumber">The offending line number of the template.</param>
        /// <returns>A formatted string stating that an uneven indent was found at a particular given line.</returns>
        public static string TagHierarchyIndentIsUneven(int lineNumber)
        {
            return string.Format(IndentIsUnevenTemplate, lineNumber);
        }
    }
}