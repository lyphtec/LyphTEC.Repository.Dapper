using Dapper;
using ServiceStack.Text;
using System.Data;

namespace LyphTEC.Repository.Dapper
{
    internal class ValueObjectHandler<T> : SqlMapper.TypeHandler<T> where T : class, IValueObject
    {

        public override T Parse(object value)
        {
            if (value == null) return null;

            var v = (string) value;

            return string.IsNullOrWhiteSpace(v)
                ? null
                : v.FromJson<T>();
        }

        public override void SetValue(IDbDataParameter parameter, T value)
        {
            if (value == null) return;

            var data = value.ToJson();

            parameter.DbType = DbType.String;
            parameter.Value = data;
        }
    }
}
