using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Susu.Application.Services;
using Susu.Domain.Entities;
using Susu.Infrastructure.Data;
using Susu.Infrastructure.Repositories;
using Xunit;

namespace Susu.Tests
{
    public class GroupServiceUnitTests
    {
        private static AppDbContext CreateInMemoryContext(string name)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task RecordContribution_Rejects_NonPositiveAmount()
        {
            var db = CreateInMemoryContext(Guid.NewGuid().ToString());
            var groupRepo = new EfRepository<Group>(db);
            var memberRepo = new EfRepository<Member>(db);
            var contribRepo = new EfRepository<Contribution>(db);
            var payoutRepo = new EfRepository<PayoutEntry>(db);
            var uow = new Susu.Infrastructure.UnitOfWork(db);
            var svc = new GroupService(groupRepo, memberRepo, contribRepo, payoutRepo, uow);

            var g = await svc.CreateGroupAsync("g1","d", 50m);
            var m = Member.Create("A","a@x", g.Id);
            await svc.AddMemberAsync(g.Id, m);

            await Assert.ThrowsAsync<ArgumentException>(async () => await svc.RecordContributionAsync(g.Id, m.Id, 0m));
            await Assert.ThrowsAsync<ArgumentException>(async () => await svc.RecordContributionAsync(g.Id, m.Id, -5m));
        }

        [Fact]
        public async Task RecordContribution_Rejects_UnknownMember()
        {
            var db = CreateInMemoryContext(Guid.NewGuid().ToString());
            var groupRepo = new EfRepository<Group>(db);
            var memberRepo = new EfRepository<Member>(db);
            var contribRepo = new EfRepository<Contribution>(db);
            var payoutRepo = new EfRepository<PayoutEntry>(db);
            var uow = new Susu.Infrastructure.UnitOfWork(db);
            var svc = new GroupService(groupRepo, memberRepo, contribRepo, payoutRepo, uow);

            var g = await svc.CreateGroupAsync("g2","d", 50m);
            var unknownMemberId = Guid.NewGuid();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.RecordContributionAsync(g.Id, unknownMemberId, 10m));
        }

        [Fact]
        public async Task AddMember_Rejects_Duplicate()
        {
            var db = CreateInMemoryContext(Guid.NewGuid().ToString());
            var groupRepo = new EfRepository<Group>(db);
            var memberRepo = new EfRepository<Member>(db);
            var contribRepo = new EfRepository<Contribution>(db);
            var payoutRepo = new EfRepository<PayoutEntry>(db);
            var uow = new Susu.Infrastructure.UnitOfWork(db);
            var svc = new GroupService(groupRepo, memberRepo, contribRepo, payoutRepo, uow);

            var g = await svc.CreateGroupAsync("g3","d", 50m);
            var m1 = Member.Create("Dup","d@x", g.Id);
            await svc.AddMemberAsync(g.Id, m1);

            var m2 = Member.Create("Dup","other@x", g.Id);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.AddMemberAsync(g.Id, m2));
        }

        [Fact]
        public async Task AddMember_Rejects_AfterPaymentsStarted()
        {
            var db = CreateInMemoryContext(Guid.NewGuid().ToString());
            var groupRepo = new EfRepository<Group>(db);
            var memberRepo = new EfRepository<Member>(db);
            var contribRepo = new EfRepository<Contribution>(db);
            var payoutRepo = new EfRepository<PayoutEntry>(db);
            var uow = new Susu.Infrastructure.UnitOfWork(db);
            var svc = new GroupService(groupRepo, memberRepo, contribRepo, payoutRepo, uow);

            var g = await svc.CreateGroupAsync("g4","d", 50m);
            var m1 = Member.Create("One","o@x", g.Id);
            await svc.AddMemberAsync(g.Id, m1);

            await svc.RecordContributionAsync(g.Id, m1.Id, 10m);

            var m2 = Member.Create("Two","t@x", g.Id);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.AddMemberAsync(g.Id, m2));
        }

        [Fact]
        public async Task RecordContribution_Rejects_Overpayment()
        {
            var db = CreateInMemoryContext(Guid.NewGuid().ToString());
            var groupRepo = new EfRepository<Group>(db);
            var memberRepo = new EfRepository<Member>(db);
            var contribRepo = new EfRepository<Contribution>(db);
            var payoutRepo = new EfRepository<PayoutEntry>(db);
            var uow = new Susu.Infrastructure.UnitOfWork(db);
            var svc = new GroupService(groupRepo, memberRepo, contribRepo, payoutRepo, uow);

            var g = await svc.CreateGroupAsync("g5","d", 50m);
            var m = Member.Create("P","p@x", g.Id);
            await svc.AddMemberAsync(g.Id, m);

            await svc.RecordContributionAsync(g.Id, m.Id, 30m);
            // attempt to pay more than owed (50 - 30 = 20)
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.RecordContributionAsync(g.Id, m.Id, 25m));
            // pay exact remaining
            await svc.RecordContributionAsync(g.Id, m.Id, 20m);
            // further payment should be rejected
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.RecordContributionAsync(g.Id, m.Id, 1m));
        }

        [Fact]
        public async Task Payout_Order_Is_By_JoinedAt()
        {
            var db = CreateInMemoryContext(Guid.NewGuid().ToString());
            var groupRepo = new EfRepository<Group>(db);
            var memberRepo = new EfRepository<Member>(db);
            var contribRepo = new EfRepository<Contribution>(db);
            var payoutRepo = new EfRepository<PayoutEntry>(db);
            var uow = new Susu.Infrastructure.UnitOfWork(db);
            var svc = new GroupService(groupRepo, memberRepo, contribRepo, payoutRepo, uow);

            var group = await svc.CreateGroupAsync("g6","d", 50m);
            var ama = Member.Create("Ama","a@x", group.Id);
            await svc.AddMemberAsync(group.Id, ama);
            var kofi = Member.Create("Kofi","k@x", group.Id);
            await svc.AddMemberAsync(group.Id, kofi);
            var esi = Member.Create("Esi","e@x", group.Id);
            await svc.AddMemberAsync(group.Id, esi);

            // everyone pays full week1
            await svc.RecordContributionAsync(group.Id, ama.Id, 50m);
            await svc.RecordContributionAsync(group.Id, kofi.Id, 50m);
            await svc.RecordContributionAsync(group.Id, esi.Id, 50m);

            var res1 = await svc.CloseWeekAsync(group.Id);
            Assert.True(res1.PaidOut);
            Assert.Equal(ama.Id, res1.Payout.RecipientId);

            // week2 everyone pays full
            await svc.RecordContributionAsync(group.Id, ama.Id, 50m);
            await svc.RecordContributionAsync(group.Id, kofi.Id, 50m);
            await svc.RecordContributionAsync(group.Id, esi.Id, 50m);

            var res2 = await svc.CloseWeekAsync(group.Id);
            Assert.True(res2.PaidOut);
            Assert.Equal(kofi.Id, res2.Payout.RecipientId);
        }
    }
}
