using System;
using System.Collections.Generic;

namespace Plugins.Warlander.Collections
{
    public class BranchTreeNode<TCategory, TValue> : ITreeNode<TCategory, TValue>
    {
        public ITreeNode<TCategory, TValue> Parent { get; }
        public TreeNodeType NodeType => TreeNodeType.Branch;
        public TValue Leaf => default;
        public TCategory Category { get; }
        public IEnumerable<ITreeNode<TCategory, TValue>> Branches => _branches.AsReadOnly();

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
        
        public ITreeNode<TCategory, TValue> AddBranch(TCategory category)
        {
            ITreeNode<TCategory, TValue> branch = new BranchTreeNode<TCategory, TValue>(this, category);
            _branches.Add(branch);
            return branch;
        }

        public ITreeNode<TCategory, TValue> AddLeaf(TValue value)
        {
            ITreeNode<TCategory, TValue> leaf = new LeafTreeNode<TCategory, TValue>(this, value);
            _branches.Add(leaf);
            return leaf;
        }

        public bool Contains(TValue value)
        {
            foreach (ITreeNode<TCategory,TValue> treeNode in _branches)
            {
                if (treeNode.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Contains(ITreeNode<TCategory, TValue> node)
        {
            if (node == this)
            {
                return true;
            }
            
            foreach (ITreeNode<TCategory,TValue> treeNode in _branches)
            {
                if (treeNode.Contains(node))
                {
                    return true;
                }
            }

            return false;
        }
    }
}