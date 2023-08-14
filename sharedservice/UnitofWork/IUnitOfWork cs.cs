﻿using sharedservice.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sharedservice.UnitofWork
{
    public interface IUnitOfWork : IDisposable
    {

        void BeginTransactionAsync();
        void CommitAsync();
        void RollbackAsync();
        void SaveChangesAsync();
        void SetPropertyValue(object entity, string propertyName, object value);
        IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class;
    }
}
