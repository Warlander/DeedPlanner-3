using System;
using System.Collections.Generic;

namespace Warlander.UI.Tree.Model
{
    public class BranchTreeNode<TCategory, TValue> : AbstractTreeNode<TCategory, TValue>
    {
        public override ITreeNode<TCategory, TValue> Parent { get; }
        public override TreeNodeType NodeType => TreeNodeType.Branch;
        public override TValue Leaf => default;
        public override bool LeafSet => false;
        public override TCategory Category { get; }
        public override IEnumerable<ITreeNode<TCategory, TValue>> Branches => _branches.AsReadOnly();

        private readonly List<ITreeNode<TCategory, TValue>> _branches = new List<ITreeNode<TCategory, TValue>>();

        public BranchTreeNode(TCategory category)
        {
            Category = category;
        }
        
        internal BranchTreeNode(ITreeNode<TCategory, TValue> parent, TCategory category)
        {
            if (parent.IsBranch() == false)
            {
                throw new ArgumentException("Parent tree node is not a branch.");
            }
            
            Parent = parent;
            Category = category;
        }
        
        public override ITreeNode<TCategory, TValue> AddBranch(TCategory category)
        {
            ITreeNode<TCategory, TValue> branch = new BranchTreeNode<TCategory, TValue>(this, category);
            branch.NodeChanged += OnNodeChanged;
            _branches.Add(branch);
            
            TriggerNodeChanged(new TreeNodeChange<TCategory, TValue>(branch, TreeNodeChangeType.Added, 1));
            
            return branch;
        }

        public override ITreeNode<TCategory, TValue> AddLeaf(TValue value)
        {
            ITreeNode<TCategory, TValue> leaf = new LeafTreeNode<TCategory, TValue>(this, value);
            leaf.NodeChanged += OnNodeChanged;
            _branches.Add(leaf);
            
            TriggerNodeChanged(new TreeNodeChange<TCategory, TValue>(leaf, TreeNodeChangeType.Added, 1));
            
            return leaf;
        }

        private void OnNodeChanged(TreeNodeChange<TCategory, TValue> change)
        {
            int propagatedLevel = change.LevelsDown + 1;
            
            TreeNodeChange<TCategory, TValue> propagatedChange =
                new TreeNodeChange<TCategory, TValue>(change.ChangedNode, change.ChangeType, propagatedLevel);
            
            TriggerNodeChanged(propagatedChange);
        }
    }
}