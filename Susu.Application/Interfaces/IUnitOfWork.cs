using System;
using System.Threading.Tasks;

namespace Susu.Application.Interfaces
{
    public interface IUnitOfWork
    {
        /// <summary>
        /// Execute the provided async action inside a database transaction.
        /// The implementation should begin a transaction, run the action, and commit; on exception it should rollback.
        /// </summary>
        Task ExecuteAsync(Func<Task> action);
    }
}
