using sharedservice.Repository;

namespace sharedservice.UnitofWork
{
    public interface IUnitOfWork : IDisposable
    {
        void BeginTransaction();

        void Commit();

        void Rollback();

        void SaveChanges();

        void SetPropertyValue(object entity, string propertyName, object value);

        IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class;
    }
}