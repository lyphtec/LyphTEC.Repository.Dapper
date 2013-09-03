using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
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

    }
}
