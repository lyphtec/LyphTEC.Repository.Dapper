using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyphTEC.Repository.Dapper.Tests
{
    public class Utils
    {
        public static string ReadScriptFile(Type type, string name)
        {
            var fileName = type.Namespace + ".Sql." + name + ".sql";
            using (var s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(fileName))
            using (var sr = new StreamReader(s))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
