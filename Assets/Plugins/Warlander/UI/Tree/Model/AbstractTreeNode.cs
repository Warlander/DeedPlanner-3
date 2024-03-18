using System;
using System.Collections.Generic;

namespace Warlander.UI.Tree.Model
{
    public abstract class AbstractTreeNode<TCategory, TValue> : ITreeNode<TCategory, TValue>
    {
        public event Action<TreeNodeChange<TCategory, TValue>> NodeChanged;
        
        public abstract ITreeNode<TCategory, TValue> Parent { get; }
        public abstract TreeNodeType NodeType { get; }
        public abstract TValue Leaf { get; }
        public abstract bool LeafSet { get; }
        public abstract TCategory Category { get; }
        public abstract IEnumerable<ITreeNode<TCategory, TValue>> Branches { get; }
        
        public abstract ITreeNode<TCategory, TValue> AddBranch(TCategory category);
        public abstract ITreeNode<TCategory, TValue> AddLeaf(TValue value);

        public virtual bool Contains(TValue value)
        {
            if (LeafSet && EqualityComparer<TValue>.Default.Equals(value , Leaf))
            {
                return true;
            }
            
            foreach (ITreeNode<TCategory,TValue> treeNode in Branches)
            {
                if (treeNode.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual bool Contains(ITreeNode<TCategory, TValue> node)
        {
            if (node == this)
            {
                return true;
            }
            
            foreach (ITreeNode<TCategory,TValue> treeNode in Branches)
            {
                if (treeNode.Contains(node))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void Select()
        {
            NodeChanged?.Invoke(new TreeNodeChange<TCategory, TValue>(this, TreeNodeChangeType.Selected, 0));
        }

        public virtual void Deselect()
        {
            NodeChanged?.Invoke(new TreeNodeChange<TCategory, TValue>(this, TreeNodeChangeType.Deselected, 0));
        }

        protected void TriggerNodeChanged(TreeNodeChange<TCategory, TValue> change)
        {
            NodeChanged?.Invoke(change);
        }
    }
}