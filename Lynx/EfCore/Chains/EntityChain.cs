using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.EfCore.Chains;

/// <summary>
/// A chain defining a reference from <see cref="Root"/> to <see cref="Target"/>
/// </summary>
public class EntityChain<T>
{
    public EntityChain(
        IEntityType root,
        IEntityType target,
        IReadOnlyList<T> chain)
    {
        Root = root;
        Target = target;
        Chain = chain;
    }

    /// <summary>The entity at the top of the chain</summary>
    public IEntityType Root { get; }

    /// <summary>The entity referenced by <see cref="Root" /></summary>
    public IEntityType Target { get; }

    /// <summary>The chain from <see cref="Root" /> to <see cref="Target" /></summary>
    public IReadOnlyList<T> Chain { get;  }

    public override string ToString() => $"{Root.Name} -> {Target.Name}: {Chain.Count} item(s)";
}