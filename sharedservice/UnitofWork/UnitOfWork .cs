

using sharedservice.Models;
using sharedservice.Repository;


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
        public void BeginTransactionAsync()
        {
            _dbContext.Database.BeginTransaction();
        }

        public void CommitAsync()
        {
             _dbContext.SaveChanges();
             _dbContext.Database.CurrentTransaction.Commit();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        public void RollbackAsync()
        {
            _dbContext.Database.CurrentTransaction.Rollback();
        }

        public void SaveChangesAsync()
        {
             _dbContext.SaveChanges();
        }

    }
}
