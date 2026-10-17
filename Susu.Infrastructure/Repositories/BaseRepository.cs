using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Susu.Application.Interfaces;
using Susu.Application.Specifications;
using Susu.Domain.Entities;

namespace Susu.Infrastructure.Repositories
{
    // Simple in-memory implementation suitable for prototyping.
    // T constrained to BaseEntity to ensure Id property exists.
    public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
    {
        private static readonly ConcurrentDictionary<Guid, T> _store = new();

        public Task<T> AddAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _store[entity.Id] = entity;
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (!_store.ContainsKey(entity.Id)) throw new KeyNotFoundException("Entity not found.");
            _store[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _store.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<T?> GetByIdAsync(Guid id)
        {
            _store.TryGetValue(id, out var entity);
            return Task.FromResult(entity);
        }

        public Task<T?> FirstOrDefaultAsync(ISpecification<T>? specification = null)
        {
            var query = _store.Values.AsQueryable();
            if (specification?.Criteria != null)
                query = query.Where(specification.Criteria);
            var result = query.FirstOrDefault();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<T>> ListAsync(ISpecification<T>? specification = null)
        {
            var query = _store.Values.AsQueryable();
            if (specification?.Criteria != null)
                query = query.Where(specification.Criteria);
            var list = query.ToList();
            return Task.FromResult((IReadOnlyList<T>)list);
        }

        public Task<int> CountAsync(ISpecification<T>? specification = null)
        {
            var query = _store.Values.AsQueryable();
            if (specification?.Criteria != null)
                query = query.Where(specification.Criteria);
            var count = query.Count();
            return Task.FromResult(count);
        }
    }
}
