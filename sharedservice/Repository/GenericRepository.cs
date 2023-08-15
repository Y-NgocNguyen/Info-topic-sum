using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace sharedservice.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly DbContext _dbContext;

        public GenericRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public void Add(T entity)
        {
            _dbContext.Set<T>().Add(entity);
        }

        public void AddRange(IEnumerable<T> entities)
        {
             _dbContext.Set<T>().AddRange(entities);
        }

        public IEnumerable<T> Find(Expression<Func<T, bool>> expression)
        {
           return _dbContext.Set<T>().Where(expression);
        }

        public IEnumerable<T> GetAll()
        {
            return _dbContext.Set<T>();
        }

        public T GetById(int id)
        {
            return _dbContext.Set<T>().Find(id);
        }

        public void Remove(T entity)
        {
            _dbContext.Set<T>().Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _dbContext.Set<T>().RemoveRange(entities);       
        }

        public void UpdateRangeOne(List<T> targetData, Dictionary<string, object> columnValues)
        {
           
            foreach (T course in targetData)
            {
                foreach (KeyValuePair<string, object> columnValue in columnValues)
                {
                    string column = columnValue.Key;
                    dynamic value = columnValue.Value;
                    PropertyInfo property = typeof(T).GetProperty(column);

                    if (value is JToken jToken && property != null)
                    {
                        value = jToken.ToObject(property.PropertyType);
                    }
                    property?.SetValue(course, value);
                }
            }
        }
        public void UpdateRangeAny(IEnumerable<T> entities)
        {
            _dbContext.Set<T>().UpdateRange(entities);
        }
        public void entry(T entity)
        {
            _dbContext.Entry(entity).State = EntityState.Detached;
        }

        /// <summary>
        /// Updates the properties of the item object with newItem object.
        /// If the value of an item property is null, it is replaced with property in newItem.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="newItem"></param>
        /// <returns></returns>
        public T update2Oject(T item,T newItem)
        {
            var itemProperties = item.GetType().GetProperties();
            var newItemProperties = newItem.GetType().GetProperties();
            List<Object> v = new List<Object>();
            foreach (var itemProperty in itemProperties)
            {
                var newItemProperty = newItemProperties.FirstOrDefault(p => p.Name == itemProperty.Name);
                if (newItemProperty != null)
                {
                    var itemValue = itemProperty.GetValue(item);
                    var newItemValue = newItemProperty.GetValue(newItem);

                   
                    if (itemValue == null)
                    { 
                        itemProperty.SetValue(item, newItemValue);
                    }
                }
            }
            return item;
        }

        public int UpdateSQLRaw(T item)
        {
            
            string elementUpdate = "";
            object elementId = "";

            PropertyInfo[] itemProperties = item.GetType().GetProperties();

            List<MySqlParameter> parameters = new List<MySqlParameter>();

            foreach (PropertyInfo property in itemProperties)
            {
                var value = property.GetValue(item);

                if (value == null) continue;

                if (property.Name.ToLower() == "id")
                {
                    elementId = value;
                    continue;
                }
                elementUpdate += $"{property.Name}=@{property.Name},";
                parameters.Add(new MySqlParameter($"@{property.Name}", value));
            }
            elementUpdate = elementUpdate.Substring(0, elementUpdate.Length - 1);

            string sql = 
                $"UPDATE {typeof(T).Name}s SET {elementUpdate} WHERE Id = @Id";
            parameters.Add(new MySqlParameter("@Id", elementId));

            return _dbContext.Database.ExecuteSqlRaw(sql, parameters);
        }
    }
}
