using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DapperExtensions.Mapper;

namespace LyphTEC.Repository.Dapper
{
    internal class CustomClassMapper<TEntity> : ClassMapper<TEntity> where TEntity : class, IEntity
    {
        public CustomClassMapper()
        {
            var type = typeof (TEntity);
            Table(type.Name);

            Map(x => x.Id).Key(KeyType.Identity);
            
            AutoMap();
        }

        void MapValueObjects()
        {
            var type = typeof (TEntity);
            var props = type.GetProperties().Where(x => IsValueObject(x.PropertyType));

            foreach (var prop in props)
            {
                // Map(...)
            }
        }

        static bool IsValueObject(Type type)
        {
            return typeof (IValueObject).IsAssignableFrom(type);
        }
    }
}
