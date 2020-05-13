using System.Collections.Generic;
using VaultLib.Core;

namespace YAMLDatabase.Core.Algorithm
{
    public class VaultDependencyNode
    {
        private sealed class VaultEqualityComparer : IEqualityComparer<VaultDependencyNode>
        {
            public bool Equals(VaultDependencyNode x, VaultDependencyNode y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Vault.Name == y.Vault.Name;
            }

            public int GetHashCode(VaultDependencyNode obj)
            {
                return (obj.Vault != null ? obj.Vault.GetHashCode() : 0);
            }
        }

        public static IEqualityComparer<VaultDependencyNode> VaultComparer { get; } = new VaultEqualityComparer();

        public List<VaultDependencyNode> Edges { get; }
        public Vault Vault { get; }

        public VaultDependencyNode(Vault vault)
        {
            Vault = vault;
            Edges = new List<VaultDependencyNode>();
        }

        public void AddEdge(VaultDependencyNode node)
        {
            Edges.Add(node);
        }
    }
}