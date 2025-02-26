using System;
using System.Collections.Generic;
using System.Composition.Hosting.Core;

namespace LyphTEC.Repository.Dapper.Tests.Extensions;

public class InstanceExportDescriptorProvider : SinglePartExportDescriptorProvider
{
    readonly object _exportedInstance;

    public InstanceExportDescriptorProvider(object exportedInstance, Type contractType, string contractName, IDictionary<string, object> metadata)
        : base(contractType, contractName, metadata)
    {
        ArgumentNullException.ThrowIfNull(exportedInstance);
        _exportedInstance = exportedInstance;
    }

    public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
    {
        if (IsSupportedContract(contract))
            yield return new ExportDescriptorPromise(contract, _exportedInstance.ToString(), true, NoDependencies, _ =>
                ExportDescriptor.Create((c, o) => _exportedInstance, Metadata));
    }
}
