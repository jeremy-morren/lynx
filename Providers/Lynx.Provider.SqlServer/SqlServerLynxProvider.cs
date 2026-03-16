using Lynx.Providers.Common;
using Lynx.Providers.Common.Entities;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.SqlServer;

internal class SqlServerLynxProvider : ILynxProvider
{
    public ILynxEntityService<TEntity> CreateService<TEntity>(IModel model) where TEntity : class
    {
        var entity = EntityInfoFactory.CreateRoot<TEntity>(model);
        return new SqlServerLynxEntityService<TEntity>(entity);
    }
}