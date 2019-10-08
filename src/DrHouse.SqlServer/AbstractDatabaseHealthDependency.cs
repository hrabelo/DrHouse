using System;
using System.Collections.Generic;
using System.Data;
using DrHouse.Core;
using DrHouse.Events;

namespace DrHouse.SqlServer
{
    public abstract class AbstractDatabaseHealthDependency : IDatabaseHealthDependency
    {
        internal string _type;
        internal string _databaseName;
        internal string _connectionString;
        internal IDictionary<string, ICollection<TablePermission>> _permissions;
        internal ICollection<Index> _indexes;
        internal IDbConnection _dbConnection;

        public event EventHandler<DependencyExceptionEvent> OnDependencyException;

        public abstract IDbConnection GetConnection();

        public void AddIndexDependency(string tableName, string indexName)
        {
            _indexes.Add(new Index { TableName = tableName, IndexName = indexName });
        }

        public void AddTableDependency(string tableName, Permission permissionSet)
        {
            if (_permissions.ContainsKey(tableName) == false)
            {
                _permissions.Add(tableName, new List<TablePermission>());
            }

            ICollection<TablePermission> tablePermisionCollection = _permissions[tableName];

            foreach (Permission permission in Enum.GetValues(typeof(Permission)))
            {
                if ((permission & permissionSet) != 0)
                {
                    tablePermisionCollection.Add(new TablePermission()
                    {
                        TableName = tableName,
                        Permission = permission,
                    });
                }
            }
        }

        public void InvokeDependencyException(Exception ex)
        {
            OnDependencyException?.Invoke(this, new DependencyExceptionEvent(ex));
        }

        public HealthData CheckHealth(Action check)
        {
            throw new NotImplementedException();
        }

        public HealthData CheckHealth()
        {
            _dbConnection = this.GetConnection();
            HealthData sqlHealthData = new HealthData(_databaseName);
            sqlHealthData.Type = _type;

            try
            {
                using (_dbConnection)
                {
                    if (_dbConnection.State != ConnectionState.Open)
                    {
                        _dbConnection.Open();
                    }

                    sqlHealthData = new HealthData(_dbConnection.Database);
                    sqlHealthData.Type = _type;

                    foreach (string tableName in _permissions.Keys)
                    {
                        HealthData tableHealth = CheckTablePermissions(tableName, _permissions[tableName]);
                        sqlHealthData.DependenciesStatus.Add(tableHealth);
                    }

                    foreach (Index ix in _indexes)
                    {
                        HealthData indexHealth = this.CheckIndex(ix);
                        sqlHealthData.DependenciesStatus.Add(indexHealth);
                    }

                    sqlHealthData.IsOK = true;
                }
            }
            catch (Exception ex)
            {
                OnDependencyException?.Invoke(this, new DependencyExceptionEvent(ex));

                sqlHealthData.IsOK = false;
                sqlHealthData.ErrorMessage = ex.Message;
            }

            return sqlHealthData;
        }

        public HealthData CheckTablePermissions(string tableName, ICollection<TablePermission> permissions)
        {
            HealthData tableHealth = new HealthData(tableName);

            try
            {
                foreach (TablePermission permission in permissions)
                {
                    HealthData tablePermissionHealth = new HealthData(permission.Permission.ToString());

                    tablePermissionHealth.IsOK = this.CheckPermission(permission);
                    if (tablePermissionHealth.IsOK == false)
                    {
                        tablePermissionHealth.ErrorMessage = "Does not have permission.";
                    }

                    tableHealth.DependenciesStatus.Add(tablePermissionHealth);
                }

                tableHealth.IsOK = true;
            }
            catch (Exception ex)
            {
                OnDependencyException?.Invoke(this, new DependencyExceptionEvent(ex));

                tableHealth.ErrorMessage = ex.Message;
                tableHealth.IsOK = false;
            }

            return tableHealth;
        }

        public abstract HealthData CheckIndex(Index index);

        public abstract bool CheckPermission(TablePermission permission);

    }
}
