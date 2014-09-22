using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DapperExtensions;
using LyphTEC.Repository.Dapper.Expressions;

namespace LyphTEC.Repository.Dapper
{
    internal static class DapperHelpers
    {
        // From http://stackoverflow.com/questions/1281161/how-to-get-the-default-value-of-a-type-if-the-type-is-only-known-as-system-type
        public static object GetDefaultValue(this Type t)
        {
            if (!t.IsValueType || Nullable.GetUnderlyingType(t) != null)
                return null;

            return Activator.CreateInstance(t);
        }

        public static IPredicate ToPredicateGroup<TEntity>(this Expression<Func<TEntity, bool>> expression) where TEntity : class, IEntity
        {
            if (expression == null)
                return null;

            var dev = new DapperExpressionVisitor<TEntity>();
            var pg = dev.Process(expression);

            return pg;
        }

        internal static void ConfigureTypeHandlers()
        {
            //SqlMapper.AddTypeHandler(IdTypeHandler.Default);

            AddValueObjectTypeHandlers(Assembly.GetEntryAssembly());
        }

        internal static void AddValueObjectTypeHandlers(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                // Scan entry assembly for all types that are IValueObject & register
                // Assumes all these types have a default empty ctor
                var iValueObjectType = typeof (IValueObject);

                var ivoTypes = assembly
                    .GetTypes()
                    .Where(
                        t =>
                            t.IsInterface == false && t.IsAbstract == false &&
                            t.GetInterfaces().Contains(iValueObjectType));

                var handler = typeof (ValueObjectHandler<>);

                foreach (var ivoType in ivoTypes)
                {
                    var ctor = handler.MakeGenericType(new[] {ivoType}).GetConstructor(Type.EmptyTypes);
                    var instance = (SqlMapper.ITypeHandler) ctor.Invoke(new object[] {});
                    SqlMapper.AddTypeHandler(ivoType, instance);
                }
            }
        }

    }
}
