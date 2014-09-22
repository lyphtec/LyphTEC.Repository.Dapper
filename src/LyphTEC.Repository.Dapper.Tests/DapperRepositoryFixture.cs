using Dapper;
using DapperExtensions.Sql;
using ServiceStack.Text;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;

namespace LyphTEC.Repository.Dapper.Tests
{
    public class DapperRepositoryFixture
    {
        private readonly string _dbConnString;
        private readonly ConnectionStringSettings _settings;

        public DapperRepositoryFixture()
        {
            AssemblyUtils.SetEntryAssembly();

            Cleanup();

            var file = string.Format("dapperRepoTest_{0}.sdf", Guid.NewGuid().ToString("N"));
            _dbConnString = string.Format("Data Source=.\\{0}", file);
            
            if (File.Exists(file))
                File.Delete(file);

            // DapperExtensions needs to know the SqlDialect if not using the default SqlServerDialect
            DapperExtensions.DapperExtensions.SqlDialect = new SqlCeDialect();

            using (var ce = new SqlCeEngine(_dbConnString))
            {
                ce.CreateDatabase();
            }

            ExecuteScript("CreateCustomerTable");

            _settings = new ConnectionStringSettings("DapperTest", _dbConnString, "System.Data.SqlServerCe.4.0");
        }

        public ConnectionStringSettings Settings
        {
            get { return _settings; }
        }

        public IDbConnection GetDbConnection()
        {
            var db = new SqlCeConnection(_dbConnString);

            return db;
        }

        int ExecuteScript(string scriptName)
        {
            var db = GetDbConnection();

            if (db.State != ConnectionState.Open)
                db.Open();

            var result = db.Execute(ReadScriptFile(scriptName));

            db.Close();

            return result;
        }

        private string ReadScriptFile(string name)
        {
            var fileName = GetType().Namespace + ".Sql." + name + ".sql";
            using (var s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(fileName))
            using (var sr = new StreamReader(s))
            {
                return sr.ReadToEnd();
            }
        }

        private static void Cleanup()
        {
            try
            {
                var files = Directory.GetFiles(Environment.CurrentDirectory, "*.sdf");
                foreach (var file in files)
                {
                    File.Delete(file);
                }

            }
            catch (Exception ex)
            {
                ex.PrintDump();
            }
        }
    }
}
