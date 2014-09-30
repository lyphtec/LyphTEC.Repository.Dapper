using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Reflection;
using Dapper;
using DapperExtensions.Sql;
using LyphTEC.Repository.Tests.Domain;
using ServiceStack.Text;

namespace LyphTEC.Repository.Dapper.Tests.SqlCe
{
    public class SqlCeRepositoryFixture
    {
        private readonly string _dbConnString;
        private readonly ConnectionStringSettings _settings;

        public SqlCeRepositoryFixture()
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

            var files = new []
            {
                "CreateCustomerTable",
                "CreateGuidEntityTable"
            };

            ExecuteScripts(files);

            _settings = new ConnectionStringSettings("DapperTest", _dbConnString, "System.Data.SqlServerCe.4.0");

            // Load any mapping assemblies - in this case, we are loading GuidEntityMapping
            var mappingAss = new []
            {
                GetType().Assembly,
                typeof(Address).Assembly  // since our test data entities live in a referenced assembly, we need to manually add it so the IValueObject can be found and registered as a type handler
            };
            DapperRepository.SetMappingAssemblies(mappingAss);
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

        void ExecuteScripts(params string[] scriptNames)
        {
            var db = GetDbConnection();

            if (db.State != ConnectionState.Open)
                db.Open();

            foreach (var scriptName in scriptNames)
            {
                db.Execute(Utils.ReadScriptFile(GetType(), scriptName));
            }

            db.Close();
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

        public GuidEntity NewGuidEntity(string name = "Bob Snob")
        {
            return new GuidEntity
            {
                Name = name,
                DateField = new DateTime(2017, 1, 1)
            };
        }
    }
}
