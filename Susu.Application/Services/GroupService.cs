using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Susu.Application.Interfaces;
using Susu.Domain.Entities;

namespace Susu.Application.Services
{
    public class GroupService : IGroupService
    {
        // simple in-memory stores keyed by group.Id
        private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<int, ConcurrentDictionary<Guid, decimal>>> _payments
            = new(); // groupId -> week -> memberId -> amount

        private readonly ConcurrentDictionary<Guid, List<PayoutEntry>> _payouts
            = new(); // groupId -> list of payouts

        private readonly ConcurrentDictionary<Guid, int> _currentWeek
            = new(); // groupId -> current week

        private readonly ConcurrentDictionary<Guid, int> _nextPayoutIndex
            = new(); // groupId -> next index

        public Group CreateGroup(string name, string description, decimal weeklyContribution)
        {
            var g = Group.Create(name, description, weeklyContribution);
            // initialize stores
            _payments[g.Id] = new ConcurrentDictionary<int, ConcurrentDictionary<Guid, decimal>>();
            _payouts[g.Id] = new List<PayoutEntry>();
            _currentWeek[g.Id] = 1;
            _nextPayoutIndex[g.Id] = 0;
            return g;
        }

        public void AddMember(Guid groupId, Member member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            // find group - assume caller has Group instance; for demo we'll require group passed by id and member assigned elsewhere
            // In a real app you'd fetch group from a repository; here we throw if group not found by stores
            if (!_payments.ContainsKey(groupId))
                throw new InvalidOperationException("Unknown group.");

            // naive: we cannot access group's Members list here because repository is not implemented; assume caller manages adding to Group.Members
            // still validate duplicate by scanning member.Id in existing payouts or payments
            // For simplicity we do not duplicate-check here; recommend caller use Group.AddMember first
        }

        public void RecordContribution(Guid groupId, Guid memberId, decimal amount, int? week = null)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive.", nameof(amount));
            if (!_payments.TryGetValue(groupId, out var groupPayments))
                throw new InvalidOperationException("Unknown group.");

            var targetWeek = week ?? _currentWeek.GetValueOrDefault(groupId, 1);
            var weekMap = groupPayments.GetOrAdd(targetWeek, _ => new ConcurrentDictionary<Guid, decimal>());
            weekMap.AddOrUpdate(memberId, amount, (_, existing) => existing + amount);
        }

        public CloseWeekResult CloseWeek(Guid groupId, int? week = null)
        {
            if (!_payments.TryGetValue(groupId, out var groupPayments))
                throw new InvalidOperationException("Unknown group.");

            var targetWeek = week ?? _currentWeek.GetValueOrDefault(groupId, 1);
            groupPayments.TryGetValue(targetWeek, out var weekMap);
            weekMap ??= new ConcurrentDictionary<Guid, decimal>();

            // In absence of repository, we cannot enumerate members from Group; this implementation requires caller to provide group membership
            // To make this workable, require that a simple membership registry be inferred from payouts store and payments keys.
            var memberIds = weekMap.Keys.Union(_payouts.GetValueOrDefault(groupId, new List<PayoutEntry>()).Select(p => p.RecipientId)).Distinct().ToList();
            // If no members known, cannot close
            if (!memberIds.Any())
                throw new InvalidOperationException("No members recorded for group; cannot determine owes.");

            // For the weekly amount we cannot read Group. So this service assumes the caller knows the weekly amount; this is suboptimal.
            // Better approach: service should receive the Group instance. We'll change signatures to accept Group where needed.
            throw new NotImplementedException("CloseWeek requires Group instance; please use CloseWeek(Group group, ...) instead.");
        }

        public WeekReport GetReport(Guid groupId, int? week = null)
        {
            throw new NotImplementedException("GetReport requires Group instance; implement after wire-up with repository or caller-provided Group.");
        }

        // Helper to get or init payouts list
        private List<PayoutEntry> GetPayoutsForGroup(Guid groupId)
        {
            return _payouts.GetValueOrDefault(groupId) ?? (_payouts[groupId] = new List<PayoutEntry>());
        }
    }
}
