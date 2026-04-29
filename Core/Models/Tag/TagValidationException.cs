namespace TagHierarchyManager.Models;

public class TagValidationException : Exception
{
    public TagValidationException() { }
    public TagValidationException(string message) : base(message) { }
    public TagValidationException(string message, Exception inner) : base(message, inner) { }
}