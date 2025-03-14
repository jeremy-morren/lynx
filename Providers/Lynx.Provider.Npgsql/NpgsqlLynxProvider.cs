using Lynx.Providers.Common;
using Lynx.Providers.Common.Entities;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.Npgsql;

internal class NpgsqlLynxProvider : ILynxProvider
{
    public ILynxDatabaseService<TEntity> CreateService<TEntity>(IModel model) where TEntity : class
    {
        var entity = EntityInfoFactory.Create(typeof(TEntity), model);
        return new NpgsqlLynxDatabaseService<TEntity>(entity);
    }
}