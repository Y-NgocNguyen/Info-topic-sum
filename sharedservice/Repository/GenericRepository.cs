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
        private readonly DbSet<T> _dbSet;

        public GenericRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<T>();
        }
        public IQueryable<T> GetAll()
        {
            return _dbSet;
        }
        public void Add(T entity)
        {
            _dbSet.Add(entity);
        }

        public void AddRange(IEnumerable<T> entities)
        {
             _dbSet.AddRange(entities);
        }

        public IQueryable<T> Find(Expression<Func<T, bool>> expression)
        {
           return _dbSet.Where(expression);
        }

        

        public T GetById(int id)
        {
            return _dbSet.Find(id);
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);       
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
            _dbSet.UpdateRange(entities);
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

        public DbContext testContext()
        {
            throw new NotImplementedException();
        }

        /*public DbContext testContext()
        {

            return _dbContext.Database;
        }*/
    }

}
