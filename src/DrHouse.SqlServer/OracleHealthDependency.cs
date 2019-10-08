using DrHouse.Core;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace DrHouse.SqlServer
{
    public class OracleHealthDependency : AbstractDatabaseHealthDependency
    {

        public OracleHealthDependency(string databaseName, string connectionString) : base()
        {
            _databaseName = databaseName;
            _connectionString = connectionString;
            _permissions = new Dictionary<string, ICollection<TablePermission>>();
            _indexes = new List<Index>();
            _type = "Oracle";
        }

        public override IDbConnection GetConnection()
        {
            return new OracleConnection(_connectionString);
        }

        public override bool CheckPermission(TablePermission permission)
        {
            string query = @"SELECT COUNT(1) FROM USER_TAB_PRIVS WHERE TABLE_BAME = @tableName AND PRIVILEGE = @permission";
            var permissionCmd = new OracleCommand(query);
            permissionCmd.Parameters.Add(new OracleParameter() { ParameterName = "@tableName", Value = permission.TableName });
            permissionCmd.Parameters.Add(new OracleParameter() { ParameterName = "@permission", Value = permission.Permission.ToString() });

            permissionCmd.Connection = _dbConnection as OracleConnection;

            var reader = permissionCmd.ExecuteReader();
            reader.Read();

            bool result = (int)reader[0] == 1;
            reader.Close();

            return result;
        }

        public override HealthData CheckIndex(Index index)
        {
            HealthData tableHealth = new HealthData(index.IndexName);
            tableHealth.ErrorMessage = $"Index '{index.IndexName}' is not yet verifiable for oracle at this point'.";
            tableHealth.IsOK = true;
            return tableHealth;
        }

    }
}
