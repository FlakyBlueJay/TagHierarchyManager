namespace TagHierarchyManager.Models;

public partial class TagDatabase
{
    /// <summary>
    ///     Event thrown when the database has successfully initialised and is ready for writing to.
    /// </summary>
    public event EventHandler InitialisationComplete = delegate { };
    
    private void OnInitialisationComplete(EventArgs e)
    {
        this.Logger.Debug("OnInitialised invoked!");
        this.InitialisationComplete.Invoke(this, e);
    }
}