using System;
using System.Data;
using Dapper;

namespace LyphTEC.Repository.Dapper;

// see https://andydote.co.uk/2014/07/22/configuring-dapper-to-work-with-custom-types/

internal class IdTypeHandler : SqlMapper.TypeHandler<dynamic>
{
    protected IdTypeHandler() { }

    public static readonly IdTypeHandler Default = new();

    public override dynamic Parse(object value)
    {
        return value;
    }

    public override void SetValue(IDbDataParameter parameter, dynamic value)
    {
        // only valid for Entity.Id field
        if (!parameter.ParameterName.Equals("Id"))
            return;

        if (value is Int32)
        {
            parameter.Value = (Int32) value;
            parameter.DbType = DbType.Int32;
        }
        else if (value is Int64)
        {
            parameter.Value = (Int64) value;
            parameter.DbType = DbType.Int64;
        }
        else if (value is Guid)
        {
            parameter.Value = (Guid) value;
            parameter.DbType = DbType.Guid;
        }
        else if (value is String)
        {
            parameter.Value = value.ToString();
            parameter.DbType = DbType.String;
        }
        else
            throw new Exception("Unsupported type for Id field. Only System.Int32, System.Int64, System.Guid, & System.String are supported");
    }
}
