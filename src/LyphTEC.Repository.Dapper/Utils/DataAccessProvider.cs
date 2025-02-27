using System;
using System.Data.Common;

namespace LyphTEC.Repository.Dapper.Utils;

internal static class DataAccessProvider
{
    public enum DataAccessProviderTypes
    {
        SqlServer,
        SqLite,
        MySql,
        PostgreSql,

#if NETSTANDARD2_1_OR_GREATER || NET462_OR_GREATER
        SqlServerV2,
        OleDb,
        SqlServerCompact
#endif
    }

    public static DbProviderFactory GetDbProviderFactory(string dbProviderFactoryTypeName, string assemblyName)
    {
        var instance = ReflectionUtils.GetStaticProperty(dbProviderFactoryTypeName, "Instance");
        if (instance == null)
        {
            var a = ReflectionUtils.LoadAssembly(assemblyName);
            if (a != null)
                instance = ReflectionUtils.GetStaticProperty(dbProviderFactoryTypeName, "Instance");
        }

        if (instance == null)
            throw new InvalidOperationException(string.Format("ERROR: {0}", dbProviderFactoryTypeName));

        return instance as DbProviderFactory;
    }

    public static DbProviderFactory GetDbProviderFactory(DataAccessProviderTypes type)
    {
        switch (type)
        {
            case DataAccessProviderTypes.SqlServer:
                return GetDbProviderFactory("System.Data.SqlClient.SqlClientFactory", "System.Data.SqlClient");
            case DataAccessProviderTypes.SqLite:
#if NETSTANDARD2_1_OR_GREATER || NET462_OR_GREATER
                return DbProviderFactories.GetFactory("System.Data.SQLite");
#else
                return GetDbProviderFactory("Microsoft.Data.Sqlite.SqliteFactory", "Microsoft.Data.Sqlite");
#endif
            case DataAccessProviderTypes.MySql:
                return GetDbProviderFactory("MySql.Data.MySqlClient.MySqlClientFactory", "MySql.Data");
            case DataAccessProviderTypes.PostgreSql:
                return GetDbProviderFactory("Npgsql.NpgsqlFactory", "Npgsql");
#if NETSTANDARD2_1_OR_GREATER || NET462_OR_GREATER
            case DataAccessProviderTypes.OleDb:
                return DbProviderFactories.GetFactory("System.Data.OleDb");
            case DataAccessProviderTypes.SqlServerCompact:
                return DbProviderFactories.GetFactory("System.Data.SqlServerCe.4.0");
            case DataAccessProviderTypes.SqlServerV2:
                return DbProviderFactories.GetFactory("Microsoft.Data.SqlClient");
#endif
            default:
                throw new NotSupportedException($"Not supported {type}");
        }
    }

    public static DbProviderFactory GetDbProviderFactory(string providerName)
    {
#if NETSTANDARD2_1_OR_GREATER || NET462_OR_GREATER
        return DbProviderFactories.GetFactory(providerName);
#else
        providerName = providerName.ToLower();

        return providerName switch
        {
            "system.data.sqlclient" => GetDbProviderFactory(DataAccessProviderTypes.SqlServer),
            "system.data.sqlite" or "microsoft.data.sqlite" => GetDbProviderFactory(DataAccessProviderTypes.SqLite),
            "mysql.data.mysqlclient" or "mysql.data" => GetDbProviderFactory(DataAccessProviderTypes.MySql),
            "npgsql" => GetDbProviderFactory(DataAccessProviderTypes.PostgreSql),
            _ => throw new NotSupportedException($"Not supported {providerName}"),
        };
#endif
    }
}