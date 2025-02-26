using System;
using System.IO;

namespace LyphTEC.Repository.Dapper.Tests;

public static class Utils
{
    public static string ReadScriptFile(Type type, string name)
    {
        var fileName = type.Namespace + ".Sql." + name + ".sql";
        using var s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(fileName);
        using var sr = new StreamReader(s);
        return sr.ReadToEnd();
    }
}
