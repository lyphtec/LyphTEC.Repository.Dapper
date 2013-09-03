using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DapperExtensions.Sql;
using ServiceStack.Text;

namespace LyphTEC.Repository.Dapper.Tests
{
    public class DapperRepositoryFixture
    {
        private IDbConnection _db;

        public IDbConnection CreateDbConnection()
        {
            Cleanup();

            var connString = string.Format("Data Source=.\\dapperRepositoryTest_{0}.sdf", Guid.NewGuid());
            var connectionParts = connString.Split(';');
            var file = connectionParts
                .ToDictionary(k => k.Split('=')[0], v => v.Split('=')[1])
                .Where(d => d.Key.Equals("Data Source", StringComparison.OrdinalIgnoreCase))
                .Select(k => k.Value).Single();

            if (File.Exists(file))
                File.Delete(file);

            // DapperExtensions needs to know the SqlDialect if not using the default SqlServerDialect
            DapperExtensions.DapperExtensions.SqlDialect = new SqlCeDialect();

            using (var ce = new SqlCeEngine(connString))
            {
                ce.CreateDatabase();
            }

            _db = new SqlCeConnection(connString);

            return _db;
        }

        public int ExecuteScript(string scriptName)
        {
            if (_db.State != ConnectionState.Open)
                _db.Open();

            var result = _db.Execute(ReadScriptFile(scriptName));

            _db.Close();

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
