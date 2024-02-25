using System.Collections.Generic;

namespace Plugins.Warlander.Collections
{
    public interface ITreeNode<TCategory, TValue>
    {
        public ITreeNode<TCategory, TValue> Parent { get; }
        public TreeNodeType NodeType { get; }
        public TValue Leaf { get; }
        public TCategory Category { get; }
        public IEnumerable<ITreeNode<TCategory, TValue>> Branches { get; }
        
        public ITreeNode<TCategory, TValue> AddBranch(TCategory category);
        public ITreeNode<TCategory, TValue> AddLeaf(TValue value);
        public bool Contains(TValue value);
        public bool Contains(ITreeNode<TCategory, TValue> node);
    }
}