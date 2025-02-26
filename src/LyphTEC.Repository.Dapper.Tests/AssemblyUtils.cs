using System;
using System.Reflection;

namespace LyphTEC.Repository.Dapper.Tests
{
    // From http://frostwave.googlecode.com/svn-history/r75/trunk/F2DUnitTests/Code/AssemblyUtilities.cs
    // More info : http://stackoverflow.com/questions/4337201/net-nunit-test-assembly-getentryassembly-is-null

    public static class AssemblyUtils
    {
        /// <summary>
        /// Use as first line in ad hoc tests (needed by XNA specifically)
        /// </summary>
        public static void SetEntryAssembly()
        {
            SetEntryAssembly(Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// Allows setting the Entry Assembly when needed. 
        /// Use AssemblyUtilities.SetEntryAssembly() as first line in XNA ad hoc tests
        /// </summary>
        /// <param name="assembly">Assembly to set as entry assembly</param>
        public static void SetEntryAssembly(Assembly assembly)
        {
            /*
            var manager = new AppDomainManager();
            var entryAssemblyfield = manager.GetType().GetField("m_entryAssembly", BindingFlags.Instance | BindingFlags.NonPublic);
            entryAssemblyfield.SetValue(manager, assembly);

            var domain = AppDomain.CurrentDomain;
            var domainManagerField = domain.GetType().GetField("_domainManager", BindingFlags.Instance | BindingFlags.NonPublic);
            domainManagerField.SetValue(domain, manager);
            */
        }

    }
}
