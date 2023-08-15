

using sharedservice.Models;
using sharedservice.Repository;
using System.Reflection;

namespace sharedservice.UnitofWork
{
     public class UnitOfWork : IUnitOfWork
    {
        private readonly courseContext _dbContext;
        
        public UnitOfWork(courseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IGenericRepository<T> GetRepository<T>() where T : class
        {
            return new GenericRepository<T>(_dbContext);
        }
        public void BeginTransaction()
        {
            _dbContext.Database.BeginTransaction();
            
        }

        public void Commit()
        {
             _dbContext.SaveChanges();
             _dbContext.Database.CurrentTransaction.Commit();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        public void Rollback()
        {
            _dbContext.Database.CurrentTransaction.Rollback();
        }

        public void SaveChanges()
        {
             _dbContext.SaveChanges();
        }
        public void SetPropertyValue(object entity, string propertyName, object value)
        {
            PropertyInfo property = entity.GetType().GetProperty(propertyName);
            if (property != null)
            {
                property.SetValue(entity, value);
            }
        }

    }
}
