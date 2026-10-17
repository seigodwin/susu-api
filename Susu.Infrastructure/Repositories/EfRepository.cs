using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Susu.Application.Interfaces;
using Susu.Application.Specifications;
using Susu.Domain.Entities;
using Susu.Infrastructure.Data;

namespace Susu.Infrastructure.Repositories
{
    public class EfRepository<T> : IBaseRepository<T> where T : class
    {
        private readonly AppDbContext _db;
        private readonly DbSet<T> _set;

        public EfRepository(AppDbContext db)
        {
            _db = db;
            _set = _db.Set<T>();
        }

        public async Task<T> AddAsync(T entity)
        {
            var entry = await _set.AddAsync(entity);
            await _db.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task UpdateAsync(T entity)
        {
            _set.Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var keyProp = _db.Model.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties.FirstOrDefault();
            if (keyProp == null) throw new InvalidOperationException("No key defined for entity");
            var keyName = keyProp.Name;
            var entity = await _set.FindAsync(new object[] { id });
            if (entity == null) return;
            _set.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public async Task<T?> GetByIdAsync(Guid id)
        {
            return await _set.FindAsync(new object[] { id }) ;
        }

        public async Task<T?> FirstOrDefaultAsync(ISpecification<T>? specification = null)
        {
            var query = ApplySpecification(_set.AsQueryable(), specification);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T>? specification = null)
        {
            var query = ApplySpecification(_set.AsQueryable(), specification);
            return await query.ToListAsync();
        }

        public async Task<int> CountAsync(ISpecification<T>? specification = null)
        {
            var query = ApplySpecification(_set.AsQueryable(), specification);
            return await query.CountAsync();
        }

        private static IQueryable<T> ApplySpecification(IQueryable<T> query, ISpecification<T>? specification)
        {
            if (specification?.Criteria != null)
                query = query.Where(specification.Criteria as Expression<Func<T, bool>>);
            return query;
        }
    }
}
