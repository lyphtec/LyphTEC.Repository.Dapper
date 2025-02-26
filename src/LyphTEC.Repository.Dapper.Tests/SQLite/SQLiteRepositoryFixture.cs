using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using Dapper;
using DapperExtensions.Sql;
using LyphTEC.Repository.Dapper.Tests.Domain;

namespace LyphTEC.Repository.Dapper.Tests.SQLite;

public class SQLiteRepositoryFixture
{
    private readonly ConnectionStringSettings _settings;

    public SQLiteRepositoryFixture()
    {
        Cleanup();

        var file = string.Format("dapperRepoTest_{0}.sqlite", Guid.NewGuid().ToString("N"));

        _settings = new ConnectionStringSettings
        {
            ConnectionString = $"Data Source=.\\{file}",
            Name = "SQLiteDb",
            ProviderName = "System.Data.SQLite"
        };

        DbProviderFactories.RegisterFactory(_settings.ProviderName, SQLiteFactory.Instance);

        if (File.Exists(file))
            File.Delete(file);

        DapperExtensions.DapperExtensions.SqlDialect = new SqliteDialect();

        var mappingAss = new[]
        {
            typeof(SqlServer.InvoiceMapper).Assembly
        };
        DapperRepository.SetMappingAssemblies(mappingAss);

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

    public SQLiteConnection CreateOpenDbConnection()
    {
        var conn = new SQLiteConnection(_settings.ConnectionString);

        if (conn.State != ConnectionState.Open)
            conn.Open();

        return conn;
    }

    private static void Cleanup()
    {
        try
        {
            var files = Directory.GetFiles(Environment.CurrentDirectory, "*.sqlite");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error cleaning up: {0}", ex.Message);
        }
    }

    void ExecuteScripts(params string[] scriptNames)
    {
        using var db = CreateOpenDbConnection();
        foreach (var scriptName in scriptNames)
        {
            db.Execute(Utils.ReadScriptFile(GetType(), scriptName));
        }
    }
}
