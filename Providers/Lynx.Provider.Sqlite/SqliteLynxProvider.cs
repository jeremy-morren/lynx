using Lynx.Providers.Common;
using Lynx.Providers.Common.Entities;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.Sqlite;

internal class SqliteLynxProvider : ILynxProvider
{
    public ILynxEntityService<TEntity> CreateService<TEntity>(IModel model) where TEntity : class
    {
        var entity = EntityInfoFactory.CreateRoot<TEntity>(model);
        return new SqliteLynxEntityService<TEntity>(entity);
    }
}