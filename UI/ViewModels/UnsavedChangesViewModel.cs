using System;

namespace TagHierarchyManager.UI.ViewModels;

public enum UnsavedChangesResult
{
    Cancel,
    Save,
    Discard
}

public partial class UnsavedChangesViewModel : ViewModelBase
{
    public void Save() => CloseAction?.Invoke(UnsavedChangesResult.Save);
    public void Discard() => CloseAction?.Invoke(UnsavedChangesResult.Discard);
    public void Cancel() => CloseAction?.Invoke(UnsavedChangesResult.Cancel);
    
    public Action<UnsavedChangesResult> CloseAction { get; set; }

    public UnsavedChangesViewModel()
    {
    }
}