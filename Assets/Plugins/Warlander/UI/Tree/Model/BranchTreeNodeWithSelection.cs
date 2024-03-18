namespace Warlander.UI.Tree.Model
{
    public class BranchTreeNodeWithSelection<TCategory, TValue> : BranchTreeNode<TCategory, TValue>
    {
        public TValue SelectedValue => HasSelection ? _selectedNode.Leaf : default;
        public bool HasSelection => _selectedNode != null;
        
        private ITreeNode<TCategory, TValue> _selectedNode;
        
        public BranchTreeNodeWithSelection() : base(default)
        {
            NodeChanged += OnNodeChanged;
        }

        public BranchTreeNodeWithSelection(TCategory category) : base(category)
        {
            
        }

        private void OnNodeChanged(TreeNodeChange<TCategory, TValue> change)
        {
            if (change.ChangeType == TreeNodeChangeType.Selected)
            {
                HandleSelectionChange(change);
            }
        }

        private void HandleSelectionChange(TreeNodeChange<TCategory, TValue> change)
        {
            if (_selectedNode != null)
            {
                _selectedNode.Deselect();
            }

            _selectedNode = change.ChangedNode;
        }
    }
}