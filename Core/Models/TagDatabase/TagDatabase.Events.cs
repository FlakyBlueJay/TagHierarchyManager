namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    /// <summary>
    ///     Event thrown when the database has successfully initialised and is ready for writing to.
    /// </summary>
    public event EventHandler InitialisationComplete = delegate { };
    
    public sealed record DatabaseEditResult(IReadOnlyList<Tag> Added, IReadOnlyList<Tag> Updated, IReadOnlyList<(int id, string name)> Deleted);
    
    public event EventHandler<DatabaseEditResult>? TagsWritten;
    
    private void OnInitialisationComplete(EventArgs e)
    {
        this.Logger.Debug("OnInitialised invoked!");
        this.InitialisationComplete.Invoke(this, e);
    }
}