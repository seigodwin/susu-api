using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Susu.Application.Interfaces;
using Susu.Application.Specifications;
using Susu.Domain.Entities;

namespace Susu.Application.Services
{
    public class GroupService : IGroupService
    {
        private readonly IBaseRepository<Group> _groupRepo;
        private readonly IBaseRepository<Member> _memberRepo;
        private readonly IBaseRepository<Contribution> _contributionRepo;
        private readonly IBaseRepository<PayoutEntry> _payoutRepo;

        public GroupService(
            IBaseRepository<Group> groupRepo,
            IBaseRepository<Member> memberRepo,
            IBaseRepository<Contribution> contributionRepo,
            IBaseRepository<PayoutEntry> payoutRepo)
        {
            _groupRepo = groupRepo;
            _memberRepo = memberRepo;
            _contributionRepo = contributionRepo;
            _payoutRepo = payoutRepo;
        }

        public async Task<Group> CreateGroupAsync(string name, string description, decimal weeklyContribution)
        {
            var group = Group.Create(name, description, weeklyContribution);
            return await _groupRepo.AddAsync(group);
        }

        public async Task AddMemberAsync(Guid groupId, Member member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            var group = await _groupRepo.GetByIdAsync(groupId) ?? throw new InvalidOperationException("Unknown group.");

            // reject duplicate by Id or Name
            if (group.Members.Any(m => m.Id == member.Id)) throw new InvalidOperationException("Member with same Id exists.");
            if (group.Members.Any(m => string.Equals(m.Name, member.Name, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Member with same name exists.");

            // rule: cannot add new members after first contribution recorded
            var contribCount = await _contributionRepo.CountAsync(new BaseSpecification<Contribution>(c => c.GroupId == groupId));
            if (contribCount > 0) throw new InvalidOperationException("Cannot add member after payments have started.");

            // assign and persist
            member.AssignToGroup(group);
            await _memberRepo.AddAsync(member);
            group.Members.Add(member);
            await _groupRepo.UpdateAsync(group);
        }

        public async Task RecordContributionAsync(Guid groupId, Guid memberId, decimal amount, int? week = null)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive.", nameof(amount));
            var group = await _groupRepo.GetByIdAsync(groupId) ?? throw new InvalidOperationException("Unknown group.");

            var member = group.Members.FirstOrDefault(m => m.Id == memberId);
            if (member == null) throw new InvalidOperationException("Unknown member or member does not belong to group.");

            var targetWeek = week ?? group.CurrentWeek;

            // sum existing contributions for this member/week
            var spec = new BaseSpecification<Contribution>(c => c.GroupId == groupId && c.MemberId == memberId && c.Week == targetWeek);
            var existing = await _contributionRepo.ListAsync(spec);
            var alreadyPaid = existing.Sum(c => c.Amount);
            var owed = group.WeeklyContribution - alreadyPaid;
            if (owed <= 0) throw new InvalidOperationException("Member has already paid in full for the week.");
            if (amount > owed) throw new InvalidOperationException("Payment exceeds amount owed for the week.");

            var contribution = new Contribution { GroupId = groupId, MemberId = memberId, Week = targetWeek, Amount = amount, PaidAt = DateTime.UtcNow };
            await _contributionRepo.AddAsync(contribution);
        }

        public async Task<CloseWeekResult> CloseWeekAsync(Guid groupId, int? week = null)
        {
            var group = await _groupRepo.GetByIdAsync(groupId) ?? throw new InvalidOperationException("Unknown group.");
            var targetWeek = week ?? group.CurrentWeek;

            var owes = new Dictionary<Guid, decimal>();
            foreach (var m in group.Members)
            {
                var spec = new BaseSpecification<Contribution>(c => c.GroupId == groupId && c.MemberId == m.Id && c.Week == targetWeek);
                var payments = await _contributionRepo.ListAsync(spec);
                var paid = payments.Sum(p => p.Amount);
                var owed = group.WeeklyContribution - paid;
                if (owed > 0) owes[m.Id] = owed;
            }

            if (owes.Count > 0)
            {
                return new CloseWeekResult { Week = targetWeek, PaidOut = false, Owes = owes, Payout = null };
            }

            if (!group.Members.Any()) throw new InvalidOperationException("No members to pay out to.");

            var pot = group.WeeklyContribution * group.Members.Count;
            if (group.NextPayoutIndex >= group.Members.Count) group.NextPayoutIndex = 0;
            var recipient = group.Members.ElementAt(group.NextPayoutIndex);

            var payout = new PayoutEntry { GroupId = groupId, Week = targetWeek, RecipientId = recipient.Id, RecipientName = recipient.Name, Amount = pot, PaidAt = DateTime.UtcNow };
            await _payoutRepo.AddAsync(payout);

            group.NextPayoutIndex = (group.NextPayoutIndex + 1) % group.Members.Count;
            if (targetWeek == group.CurrentWeek) group.CurrentWeek++;
            await _groupRepo.UpdateAsync(group);

            return new CloseWeekResult { Week = targetWeek, PaidOut = true, Owes = new Dictionary<Guid, decimal>(), Payout = payout };
        }

        public async Task<WeekReport> GetReportAsync(Guid groupId, int? week = null)
        {
            var group = await _groupRepo.GetByIdAsync(groupId) ?? throw new InvalidOperationException("Unknown group.");
            var targetWeek = week ?? group.CurrentWeek;

            var paidInFull = new List<Guid>();
            var owes = new Dictionary<Guid, decimal>();

            foreach (var m in group.Members)
            {
                var spec = new BaseSpecification<Contribution>(c => c.GroupId == groupId && c.MemberId == m.Id && c.Week == targetWeek);
                var payments = await _contributionRepo.ListAsync(spec);
                var paid = payments.Sum(p => p.Amount);
                var owed = group.WeeklyContribution - paid;
                if (owed <= 0) paidInFull.Add(m.Id);
                else owes[m.Id] = owed;
            }

            var payoutHistory = await _payoutRepo.ListAsync(new BaseSpecification<PayoutEntry>(p => p.GroupId == groupId));

            return new WeekReport { Week = targetWeek, PaidInFull = paidInFull, Owes = owes, PayoutHistory = payoutHistory.ToList() };
        }
    }
}
