namespace Warlander.UI.Tree.Model
{
    public static class TreeNodeExtensions
    {
        public static bool IsBranch<TCategory, TValue>(this ITreeNode<TCategory, TValue> treeNode)
        {
            return treeNode.NodeType == TreeNodeType.Branch || treeNode.NodeType == TreeNodeType.BranchLeaf;
        }
        
        public static bool IsLeaf<TCategory, TValue>(this ITreeNode<TCategory, TValue> treeNode)
        {
            return treeNode.NodeType == TreeNodeType.Leaf || treeNode.NodeType == TreeNodeType.BranchLeaf;
        }
    }
}