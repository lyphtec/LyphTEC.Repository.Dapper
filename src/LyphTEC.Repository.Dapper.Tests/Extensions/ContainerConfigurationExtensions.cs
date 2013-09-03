using System;
using System.Collections.Generic;
using System.Composition.Hosting;

// From http://mef.codeplex.com/SourceControl/changeset/view/24db23e5045a#oob/demo/Microsoft.Composition.Demos.ExtendedPartTypes/Extension/ContainerConfigurationExtensions.cs

namespace LyphTEC.Repository.Dapper.Tests.Extensions
{
    public static class ContainerConfigurationExtensions
    {
        public static ContainerConfiguration WithExport<T>(this ContainerConfiguration configuration, T exportedInstance, string contractName = null, IDictionary<string, object> metadata = null)
        {
            return WithExport(configuration, exportedInstance, typeof(T), contractName, metadata);
        }

        public static ContainerConfiguration WithExport(this ContainerConfiguration configuration, object exportedInstance, Type contractType, string contractName = null, IDictionary<string, object> metadata = null)
        {
            return configuration.WithProvider(new InstanceExportDescriptorProvider(
                exportedInstance, contractType, contractName, metadata));
        }

        public static ContainerConfiguration WithFactoryDelegate<T>(this ContainerConfiguration configuration, Func<T> exportedInstanceFactory, string contractName = null, IDictionary<string, object> metadata = null, bool isShared = false)
        {
            return WithFactoryDelegate(configuration, () => exportedInstanceFactory(), typeof(T), contractName, metadata, isShared);
        }

        public static ContainerConfiguration WithFactoryDelegate(this ContainerConfiguration configuration, Func<object> exportedInstanceFactory, Type contractType, string contractName = null, IDictionary<string, object> metadata = null, bool isShared = false)
        {
            return configuration.WithProvider(new DelegateExportDescriptorProvider(
                exportedInstanceFactory, contractType, contractName, metadata, isShared));
        }
    }
}
