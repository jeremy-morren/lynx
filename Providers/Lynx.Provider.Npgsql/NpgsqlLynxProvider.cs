using Lynx.Providers.Common;
using Lynx.Providers.Common.Entities;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.Npgsql;

internal class NpgsqlLynxProvider : ILynxProvider
{
    public ILynxEntityService<TEntity> CreateService<TEntity>(IModel model) where TEntity : class
    {
        var entity = RootEntityInfoFactory.Create<TEntity>(model);
        return new NpgsqlLynxEntityService<TEntity>(entity);
    }
}