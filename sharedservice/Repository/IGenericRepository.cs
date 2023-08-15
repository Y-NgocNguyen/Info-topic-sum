﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace sharedservice.Repository
{
    public interface IGenericRepository<T> where T : class
    {
        T GetById(int id);
        IEnumerable<T> GetAll();
        IEnumerable<T> Find(Expression<Func<T, bool>> expression);
        void Add(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        void AddRange(IEnumerable<T> entities);
 
        void UpdateRangeOne(List<T> targetData, Dictionary<string, object> columnValues);
        void UpdateRangeAny(IEnumerable<T> entities);
        void entry(T entity);
         T update2Oject(T item, T newItem);
        int UpdateSQLRaw(T item);
    }
}
