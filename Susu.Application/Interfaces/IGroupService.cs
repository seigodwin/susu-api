using System;
using System.Collections.Generic;
using Susu.Domain.Entities;

namespace Susu.Application.Interfaces
{
    public interface IGroupService
    {
        System.Threading.Tasks.Task<Group> CreateGroupAsync(string name, string description, decimal weeklyContribution);
        System.Threading.Tasks.Task AddMemberAsync(Guid groupId, Member member);
        System.Threading.Tasks.Task RecordContributionAsync(Guid groupId, Guid memberId, decimal amount, int? week = null);
        System.Threading.Tasks.Task<CloseWeekResult> CloseWeekAsync(Guid groupId, int? week = null);
        System.Threading.Tasks.Task<WeekReport> GetReportAsync(Guid groupId, int? week = null);
    }

    public sealed class CloseWeekResult
    {
        public int Week { get; init; }
        public bool PaidOut { get; init; }
        public Dictionary<Guid, decimal> Owes { get; init; } = new Dictionary<Guid, decimal>();
        public Susu.Domain.Entities.PayoutEntry? Payout { get; init; }
    }

    public sealed class WeekReport
    {
        public int Week { get; init; }
        public List<Guid> PaidInFull { get; init; } = new List<Guid>();
        public Dictionary<Guid, decimal> Owes { get; init; } = new Dictionary<Guid, decimal>();
        public List<Susu.Domain.Entities.PayoutEntry> PayoutHistory { get; init; } = new List<Susu.Domain.Entities.PayoutEntry>();
    }
}
