using System.Collections.Concurrent;
using Lynx.Providers.Common;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.DocumentStore.Providers;

/// <summary>
/// A wrapper around an <see cref="ILynxProvider"/> that caches services for a given model.
/// </summary>
internal class CachingLynxProvider
{
    private readonly IModel _model;
    private readonly ILynxProvider _provider;

    public CachingLynxProvider(IModel model, ILynxProvider provider)
    {
        _model = model;
        _provider = provider;
    }
    
    private readonly ConcurrentDictionary<Type, object> _services = new();

    public ILynxEntityService<TEntity> GetService<TEntity>() where TEntity : class
    {
        var result = _services.GetOrAdd(typeof(TEntity), _ => _provider.CreateService<TEntity>(_model));
        return (ILynxEntityService<TEntity>) result;
    }
}