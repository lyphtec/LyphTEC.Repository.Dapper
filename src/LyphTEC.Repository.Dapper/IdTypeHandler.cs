using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace LyphTEC.Repository.Dapper
{
    // see http://andydote.co.uk/configuring-dapper-to-work-with-custom-types/

    internal class IdTypeHandler : SqlMapper.TypeHandler<dynamic>
    {
        protected IdTypeHandler() { }

        public static readonly IdTypeHandler Default = new IdTypeHandler();

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
}
