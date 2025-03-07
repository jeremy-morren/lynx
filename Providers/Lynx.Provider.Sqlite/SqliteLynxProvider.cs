using Lynx.Provider.Common;
using Lynx.Provider.Common.Entities;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.Sqlite;

internal class SqliteLynxProvider : ILynxProvider
{
    public ILynxDatabaseService<TEntity> CreateService<TEntity>(IModel model) where TEntity : class
    {
        var entity = EntityInfoFactory.Create(typeof(TEntity), model);
        return new SqliteLynxDatabaseService<TEntity>(entity);
    }
}