using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;

namespace LyphTEC.Repository.Dapper.Tests;

internal class ShallowSpecimen : ISpecimenBuilder
{
    private static readonly Type[] AllowedTypes =
    [
        typeof(string),
        typeof(DateTime)
    ];

    public object Create(object request, ISpecimenContext context)
    {
        if (request is PropertyInfo { PropertyType: { IsClass: true } or { IsInterface: true } } prop && !AllowedTypes.Contains(prop.PropertyType))
            return new OmitSpecimen();

        return new NoSpecimen();
    }
}

internal class ShallowBuild : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new ShallowSpecimen());
    }
}

internal class EnumDefaulter : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is PropertyInfo prop && prop.PropertyType.IsEnum)
            return new OmitSpecimen();

        return new NoSpecimen();
    }
}

internal class DefaultEnums : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new EnumDefaulter());
    }
}

internal class NoRecursion : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
}

public static class Generator
{
    private static readonly IFixture Shallow = new Fixture()
        .Customize(new NoRecursion())
        .Customize(new DefaultEnums())
        .Customize(new ShallowBuild());

    private static readonly IFixture Deep = new Fixture()
        .Customize(new NoRecursion())
        .Customize(new DefaultEnums());

    public static TEntity Generate<TEntity>(Action<TEntity> updates = null)
    {
        var result = Shallow.Create<TEntity>();

        updates?.Invoke(result);

        return result;
    }

    public static IEnumerable<TEntity> Generate<TEntity>(int count, Action<TEntity> updates = null)
    {
        var results = Shallow.CreateMany<TEntity>(count);

        foreach (var result in results)
        {
            updates?.Invoke(result);
        }

        return results;
    }

    public static TEntity DeepGenerate<TEntity>(Action<TEntity> updates = null)
    {
        var result = Deep.Create<TEntity>();

        updates?.Invoke(result);

        return result;
    }

}
