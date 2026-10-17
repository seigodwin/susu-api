using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Susu.Application.Interfaces;
using Susu.Infrastructure.Data;

namespace Susu.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;

        public UnitOfWork(AppDbContext db)
        {
            _db = db;
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            // Ensure the provided action runs within a database transaction
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                await action();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
