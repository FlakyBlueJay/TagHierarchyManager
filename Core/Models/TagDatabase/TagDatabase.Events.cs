namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    /// <summary>
    ///     Event thrown when the database has successfully initialised and is ready for writing to.
    /// </summary>
    public event EventHandler InitialisationComplete = delegate { };
    
    public sealed record DatabaseEditResult(List<Tag> Added, List<Tag> Updated, List<(int id, string name)> Deleted);
    
    public event EventHandler<DatabaseEditResult>? TagsWritten;
    
    public event EventHandler<(int id, string name)> TagDeleted = delegate { };
    
    public event EventHandler<List<Tag>> TagsAdded = delegate { };
    
    private void OnInitialisationComplete(EventArgs e)
    {
        this.Logger.Debug("OnInitialised invoked!");
        this.InitialisationComplete.Invoke(this, e);
    }
}