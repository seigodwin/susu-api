using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Susu.Application.Interfaces;
using Susu.Application.Services;
using Susu.Domain.Entities;
using Susu.Infrastructure.Data;
using Susu.Infrastructure.Repositories;
using Xunit;

namespace Susu.Tests
{
    public class IntegrationTests
    {
        private static AppDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task AmaKofiEsi_Scenario_WorksAsExpected()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateInMemoryContext(dbName);

            var groupRepo = new EfRepository<Group>(context);
            var memberRepo = new EfRepository<Member>(context);
            var contribRepo = new EfRepository<Contribution>(context);
            var payoutRepo = new EfRepository<PayoutEntry>(context);
            var unitOfWork = new Susu.Infrastructure.UnitOfWork(context);

            var service = new GroupService(groupRepo, memberRepo, contribRepo, payoutRepo, unitOfWork);

            // 1. Create group weekly 50
            var group = await service.CreateGroupAsync("TestGroup", "desc", 50.00m);

            // 2. Add members Ama, Kofi, Esi
            var ama = Member.Create("Ama", "ama@example.com", group.Id);
            await service.AddMemberAsync(group.Id, ama);

            var kofi = Member.Create("Kofi", "kofi@example.com", group.Id);
            await service.AddMemberAsync(group.Id, kofi);

            var esi = Member.Create("Esi", "esi@example.com", group.Id);
            await service.AddMemberAsync(group.Id, esi);

            // 3. Record contributions Week1: Ama 50, Kofi 50, Esi 30
            await service.RecordContributionAsync(group.Id, ama.Id, 50.00m);
            await service.RecordContributionAsync(group.Id, kofi.Id, 50.00m);
            await service.RecordContributionAsync(group.Id, esi.Id, 30.00m);

            // 4. Close week1 -> should NOT pay out and report Esi owes 20
            var result1 = await service.CloseWeekAsync(group.Id);
            Assert.False(result1.PaidOut);
            Assert.True(result1.Owes.ContainsKey(esi.Id));
            Assert.Equal(20.00m, result1.Owes[esi.Id]);

            // Esi pays remaining 20
            await service.RecordContributionAsync(group.Id, esi.Id, 20.00m);

            // Close week1 again -> should pay out to Ama (first member)
            var result2 = await service.CloseWeekAsync(group.Id);
            Assert.True(result2.PaidOut);
            Assert.NotNull(result2.Payout);
            Assert.Equal(150.00m, result2.Payout.Amount);
            Assert.Equal(ama.Id, result2.Payout.RecipientId);

            // After payout, week should advance and next recipient should be Kofi
            var updatedGroup = await groupRepo.GetByIdAsync(group.Id);
            Assert.Equal(2, updatedGroup.CurrentWeek);
            Assert.Equal(1, updatedGroup.NextPayoutIndex);
        }
    }
}
