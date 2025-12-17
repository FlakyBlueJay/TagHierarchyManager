using TagHierarchyManager.Models;
using Terminal.Gui.Views;

namespace TagHierarchyManager.UI.TerminalUI.Views;

internal class TagHierarchyTreeNode(Tag tag) : TreeNode
{
    public Tag AssociatedTag { get; } = tag;
    public override IList<ITreeNode> Children => this.GetChildren().ToList();
    public bool IsChildNode { get; private init; }
    public override string Text => this.AssociatedTag.ToString();

    private IEnumerable<ITreeNode> GetChildren()
    {
        if (Program.CurrentDatabase is null) return [];

        IEnumerable<TagHierarchyTreeNode> children =
            Program.CurrentDatabase.Tags
                .Where(tag => tag.ParentIds.Contains(this.AssociatedTag.Id))
                .Select(tag => new TagHierarchyTreeNode(tag)
                {
                    IsChildNode = true,
                })
                .OrderBy(tagNode => tagNode.AssociatedTag.Name, StringComparer.CurrentCultureIgnoreCase);
        return children;
    }
}