using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using Dapper;
using DapperExtensions.Sql;
using Microsoft.Data.SqlClient;

namespace LyphTEC.Repository.Dapper.Tests.SqlServer;

public class SqlServerRepositoryFixture
{
    private readonly ConnectionStringSettings _settings;

    public SqlServerRepositoryFixture()
    {
        _settings = new ConnectionStringSettings
        {
            ConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=DapperRepoTest;Integrated Security=true;Application Name=LyphTEC.Repository.Dapper.Tests;",
            Name = "RepoTest",
            ProviderName = "Microsoft.Data.SqlClient"
        };

        DbProviderFactories.RegisterFactory(_settings.ProviderName, Microsoft.Data.SqlClient.SqlClientFactory.Instance);

        // being explicit here, but this should be default
        DapperExtensions.DapperExtensions.SqlDialect = new SqlServerDialect();

        var mappingAss = new[]
        {
            typeof(InvoiceMapper).Assembly
        };
        DapperRepository.SetMappingAssemblies(mappingAss);

        Cleanup();

        var scripts = new[]
        {
            "CreateCustomerTable",
            "CreateInvoiceTable"
        };

        ExecuteScripts(scripts);
    }

    public ConnectionStringSettings Settings
    {
        get { return _settings; }
    }

    public IDbConnection CreateOpenDbConnection()
    {
        var db = new SqlConnection(_settings.ConnectionString);

        if (db.State != ConnectionState.Open)
            db.Open();

        return db;
    }

    private void Cleanup()
    {
        try
        {
            using var db = CreateOpenDbConnection();
            db.Execute("drop table [Customer]; drop table [Invoice];");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    void ExecuteScripts(params string[] scriptNames)
    {
        using var db = CreateOpenDbConnection();
        if (db.State != ConnectionState.Open)
            db.Open();

        foreach (var scriptName in scriptNames)
        {
            db.Execute(Utils.ReadScriptFile(GetType(), scriptName));
        }

        db.Close();
    }
}
