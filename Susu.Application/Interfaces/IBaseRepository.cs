using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Susu.Application.Specifications;

namespace Susu.Application.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(Guid id);
        Task<T?> GetByIdAsync(Guid id);
        Task<T?> FirstOrDefaultAsync(ISpecification<T>? specification = null);
        Task<IReadOnlyList<T>> ListAsync(ISpecification<T>? specification = null);
        Task<int> CountAsync(ISpecification<T>? specification = null);
    }
}
