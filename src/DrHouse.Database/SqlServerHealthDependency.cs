using DrHouse.Core;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using DrHouse.Events;
using System.Data;

namespace DrHouse.SqlServer
{
    public class SqlServerHealthDependency : AbstractDatabaseHealthDependency
    {

        public SqlServerHealthDependency(string databaseName, string connectionString) : base()
        {
            _databaseName = databaseName;
            _connectionString = connectionString;
            _permissions = new Dictionary<string, ICollection<TablePermission>>();
            _indexes = new List<Index>();
            _type = "SqlServer";
        }

        public override IDbConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public override bool CheckPermission(TablePermission permission)
        {
            string query = @"SELECT HAS_PERMS_BY_NAME (@tableName, 'OBJECT', @permission)";
            var permissionCmd = new SqlCommand(query);
            permissionCmd.Parameters.Add(new SqlParameter() { ParameterName = "@tableName", Value = permission.TableName });
            permissionCmd.Parameters.Add(new SqlParameter() { ParameterName = "@permission", Value = permission.Permission.ToString() });

            permissionCmd.Connection = _dbConnection as SqlConnection;

            var reader = permissionCmd.ExecuteReader();
            reader.Read();

            bool result = (int)reader[0] == 1;
            reader.Close();

            return result;
        }

        public override HealthData CheckIndex(Index index)
        {
            HealthData tableHealth = new HealthData(index.IndexName);

            string query = @"SELECT COUNT(1) FROM sys.indexes WHERE name = @indexName AND object_id = OBJECT_ID(@tableName)";

            try
            {
                var permissionCmd = new SqlCommand(query);
                permissionCmd.Parameters.Add(new SqlParameter() { ParameterName = "@indexName", Value = index.IndexName });
                permissionCmd.Parameters.Add(new SqlParameter() { ParameterName = "@tableName", Value = index.TableName });

                permissionCmd.Connection = _dbConnection as SqlConnection;

                bool result = false;
                using (var reader = permissionCmd.ExecuteReader())
                {
                    reader.Read();

                    // If there is at lease one, return success
                    result = (int)reader[0] > 0;
                }

                if (tableHealth.IsOK == false)
                {
                    tableHealth.ErrorMessage = $"Index '{index.IndexName}' not found for table '{index.TableName}'.";
                }

                tableHealth.IsOK = result;
            }
            catch (Exception ex)
            {
                base.InvokeDependencyException(ex);

                tableHealth.ErrorMessage = ex.Message;
                tableHealth.IsOK = false;
            }

            return tableHealth;
        }

    }
}
