using DrHouse.Core;
using System.Collections.Generic;
using System.Data;

namespace DrHouse.SqlServer
{
    public interface IDatabaseHealthDependency : IHealthDependency
    {
        IDbConnection GetConnection();

        void AddTableDependency(string tableName, Permission permissionSet);

        void AddIndexDependency(string tableName, string indexName);

        HealthData CheckTablePermissions(string tableName, ICollection<TablePermission> permissions);

        bool CheckPermission(TablePermission permission );

        HealthData CheckIndex(Index index);

    }
}
