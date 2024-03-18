using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlander.UI.Tree.Model
{
    public class LeafTreeNode<TCategory, TValue> : AbstractTreeNode<TCategory, TValue>
    {
        public override ITreeNode<TCategory, TValue> Parent { get; }
        public override TreeNodeType NodeType => TreeNodeType.Leaf;
        public override TValue Leaf { get; }
        public override bool LeafSet => true;
        public override TCategory Category => Parent != null ? Parent.Category : default;
        public override IEnumerable<ITreeNode<TCategory, TValue>> Branches => Enumerable.Empty<ITreeNode<TCategory, TValue>>();

        internal LeafTreeNode(ITreeNode<TCategory, TValue> parent, TValue value)
        {
            if (parent.IsBranch() == false)
            {
                throw new ArgumentException("Parent tree node is not a branch.");
            }

            Parent = parent;
            Leaf = value;
        }
        
        public override ITreeNode<TCategory, TValue> AddBranch(TCategory category)
        {
            throw new InvalidOperationException("Unable to add branch to a leaf node.");
        }

        public override ITreeNode<TCategory, TValue> AddLeaf(TValue value)
        {
            throw new InvalidOperationException("Unable to add leaf to a leaf node.");
        }
    }
}