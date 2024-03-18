namespace Warlander.UI.Tree.Model
{
    public class TreeNodeChange<TCategory, TValue>
    {
        public ITreeNode<TCategory, TValue> ChangedNode { get; }
        public TreeNodeChangeType ChangeType { get; }
        public int LevelsDown { get; }

        public bool SelfChanged => LevelsDown == 0;
        public bool LeafChanged => LevelsDown == 1;

        public TreeNodeChange(ITreeNode<TCategory, TValue> changedNode, TreeNodeChangeType changeType, int levelsDown)
        {
            ChangedNode = changedNode;
            ChangeType = changeType;
            LevelsDown = levelsDown;
        }
    }
}