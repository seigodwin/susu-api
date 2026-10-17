using System;
using System.Linq.Expressions;

namespace Susu.Application.Specifications
{
    public interface ISpecification<T>
    {
        Expression<Func<T, bool>>? Criteria { get; }
    }
}
