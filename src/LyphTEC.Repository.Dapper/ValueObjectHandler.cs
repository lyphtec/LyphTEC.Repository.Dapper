using Dapper;
using System.Data;
using System.Text.Json;

namespace LyphTEC.Repository.Dapper;

internal class ValueObjectHandler<T> : SqlMapper.TypeHandler<T> where T : class, IValueObject
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public override T Parse(object value)
    {
        if (value == null) return null;

        var v = (string) value;

        return string.IsNullOrWhiteSpace(v)
            ? null
            : JsonSerializer.Deserialize<T>(v, _options);
    }

    public override void SetValue(IDbDataParameter parameter, T value)
    {
        if (value == null) return;

        var data = JsonSerializer.Serialize(value, _options);

        parameter.DbType = DbType.String;
        parameter.Value = data;
    }
}
