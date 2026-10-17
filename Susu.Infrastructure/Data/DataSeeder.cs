using System;
using System.Linq;
using System.Threading.Tasks;
using Susu.Application.Interfaces;
using Susu.Application.Specifications;
using Susu.Domain.Entities;

namespace Susu.Infrastructure.Data
{
    // Seeds demo data (Ama, Kofi, Esi) when database is empty.
    public class DataSeeder
    {
        private readonly IBaseRepository<Group> _groupRepo;
        private readonly IBaseRepository<Member> _memberRepo;
        private readonly IBaseRepository<Contribution> _contribRepo;

        public DataSeeder(IBaseRepository<Group> groupRepo, IBaseRepository<Member> memberRepo, IBaseRepository<Contribution> contribRepo)
        {
            _groupRepo = groupRepo;
            _memberRepo = memberRepo;
            _contribRepo = contribRepo;
        }

        public async Task SeedDemoAsync()
        {
            var existing = await _groupRepo.CountAsync(null);
            if (existing > 0) return; // already seeded or has data

            // Create example group
            var group = Group.Create("Demo Susu", "Demo group seeded by DataSeeder", 50.00m);
            await _groupRepo.AddAsync(group);

            // Create members in join order Ama, Kofi, Esi
            var ama = Member.Create("Ama", "ama@example.com", group.Id);
            ama.AssignToGroup(group);
            await _memberRepo.AddAsync(ama);

            var kofi = Member.Create("Kofi", "kofi@example.com", group.Id);
            kofi.AssignToGroup(group);
            await _memberRepo.AddAsync(kofi);

            var esi = Member.Create("Esi", "esi@example.com", group.Id);
            esi.AssignToGroup(group);
            await _memberRepo.AddAsync(esi);

            // Record partial contributions for week 1: Ama 50, Kofi 50, Esi 30
            await _contribRepo.AddAsync(new Contribution { GroupId = group.Id, MemberId = ama.Id, Week = 1, Amount = 50.00m });
            await _contribRepo.AddAsync(new Contribution { GroupId = group.Id, MemberId = kofi.Id, Week = 1, Amount = 50.00m });
            await _contribRepo.AddAsync(new Contribution { GroupId = group.Id, MemberId = esi.Id, Week = 1, Amount = 30.00m });
        }
    }
}
