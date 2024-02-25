using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugins.Warlander.Collections
{
    public class LeafTreeNode<TCategory, TValue> : ITreeNode<TCategory, TValue>
    {
        public ITreeNode<TCategory, TValue> Parent { get; }
        public TreeNodeType NodeType => TreeNodeType.Leaf;
        public TValue Leaf { get; }
        public TCategory Category => Parent != null ? Parent.Category : default;
        public IEnumerable<ITreeNode<TCategory, TValue>> Branches => Enumerable.Empty<ITreeNode<TCategory, TValue>>();

        internal LeafTreeNode(ITreeNode<TCategory, TValue> parent, TValue value)
        {
            if (parent.IsBranch() == false)
            {
                throw new ArgumentException("Parent tree node is not a branch.");
            }

            Parent = parent;
            Leaf = value;
        }
        
        public ITreeNode<TCategory, TValue> AddBranch(TCategory category)
        {
            throw new InvalidOperationException("Unable to add branch to a leaf node.");
        }

        public ITreeNode<TCategory, TValue> AddLeaf(TValue value)
        {
            throw new InvalidOperationException("Unable to add leaf to a leaf node.");
        }

        public bool Contains(TValue value)
        {
            return EqualityComparer<TValue>.Default.Equals(value , Leaf);
        }

        public bool Contains(ITreeNode<TCategory, TValue> node)
        {
            return node == this;
        }
    }
}