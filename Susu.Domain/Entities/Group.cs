using System;
using System.Collections.Generic;
using System.Text;

namespace Susu.Domain.Entities
{
    public sealed class Group : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public decimal WeeklyContribution { get; private set; }
        public ICollection<Member> Members { get; private set; } = new List<Member>();

        // Current week number, starts at 1
        public int CurrentWeek { get; private set; } = 1;

        // Payments recorded per week -> memberId -> amount paid
        private readonly Dictionary<int, Dictionary<Guid, decimal>> _paymentsByWeek = new();

        // Payout history (successful payouts when a week is closed)
        public List<PayoutEntry> PayoutHistory { get; private set; } = new List<PayoutEntry>();

        // Index of next member to receive the pot (round-robin)
        private int _nextPayoutIndex = 0;
     

        public static Group Create(string name, string description)
        {
            var group =  new Group
            {
                Name = name,
                Description = description,
            };

            group.MarkDateCreated();

            return group;
        }
        
        /// <summary>
        /// Add a member to the group. Members must be unique by Id and Name.
        /// Assumption: adding members is allowed only before any payments have been recorded.
        /// </summary>
        public void AddMember(Member member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            // reject duplicate Id or Name
            foreach (var m in Members)
            {
                if (m.Id == member.Id)
                    throw new InvalidOperationException("Member with the same Id already exists.");
                if (string.Equals(m.Name, member.Name, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Member with the same name already exists.");
            }

            Members.Add(member);
            member.AssignToGroup(this);
            UpdateTimestamp();
        }
        public void RemoveMember(Member member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            Members.Remove(member);
            UpdateTimestamp();
        }
        public void UpdateGroupDetails(string name, string description)
        {
            Name = name;
            Description = description;
            UpdateTimestamp();
        }

        /// <summary>
        /// Create a group with a fixed weekly contribution amount.
        /// </summary>
        public static Group Create(string name, string description, decimal weeklyContribution)
        {
            if (weeklyContribution <= 0)
                throw new ArgumentException("Weekly contribution must be positive.", nameof(weeklyContribution));

            var group = Create(name, description);
            group.WeeklyContribution = weeklyContribution;
            return group;
        }

        /// <summary>
        /// Record a contribution for a member in the current week. Partial payments allowed.
        /// </summary>
        public void RecordContribution(Guid memberId, decimal amount, int? week = null)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive.", nameof(amount));

            var member = Members.FirstOrDefault(m => m.Id == memberId);
            if (member == null)
                throw new InvalidOperationException("Unknown member.");

            var targetWeek = week ?? CurrentWeek;
            if (!_paymentsByWeek.TryGetValue(targetWeek, out var weekPayments))
            {
                weekPayments = new Dictionary<Guid, decimal>();
                _paymentsByWeek[targetWeek] = weekPayments;
            }

            weekPayments.TryGetValue(memberId, out var alreadyPaid);
            var owed = WeeklyContribution - alreadyPaid;
            if (owed <= 0)
                throw new InvalidOperationException("Member has already paid in full for the week.");
            if (amount > owed)
                throw new InvalidOperationException("Payment exceeds amount owed for the week.");

            // exact money handling using decimal
            weekPayments[memberId] = alreadyPaid + amount;
            UpdateTimestamp();
        }

        /// <summary>
        /// Close the specified week (defaults to CurrentWeek). If every member has paid in full,
        /// the pot is paid to the next member in order and the week advances. Otherwise, returns who owes and how much.
        /// </summary>
        public CloseWeekResult CloseWeek(int? week = null)
        {
            var targetWeek = week ?? CurrentWeek;

            // prepare payments map for the week
            _paymentsByWeek.TryGetValue(targetWeek, out var weekPayments);
            weekPayments ??= new Dictionary<Guid, decimal>();

            // check who owes
            var owes = new Dictionary<Guid, decimal>();
            foreach (var m in Members)
            {
                weekPayments.TryGetValue(m.Id, out var paid);
                var owed = WeeklyContribution - paid;
                if (owed > 0)
                    owes[m.Id] = owed;
            }

            if (owes.Count > 0)
            {
                return new CloseWeekResult
                {
                    PaidOut = false,
                    Owes = owes,
                    Payout = null,
                    Week = targetWeek
                };
            }

            // all paid in full -> pay pot to next member
            if (Members.Count == 0)
                throw new InvalidOperationException("No members to pay out to.");

            var pot = WeeklyContribution * Members.Count;

            // ensure _nextPayoutIndex within bounds
            if (_nextPayoutIndex >= Members.Count)
                _nextPayoutIndex = 0;

            var recipient = Members.ElementAt(_nextPayoutIndex);
            var payout = new PayoutEntry
            {
                Week = targetWeek,
                RecipientId = recipient.Id,
                RecipientName = recipient.Name,
                Amount = pot,
                PaidAt = DateTime.UtcNow
            };

            PayoutHistory.Add(payout);
            _nextPayoutIndex = (_nextPayoutIndex + 1) % Members.Count;

            // advance week
            if (targetWeek == CurrentWeek)
                CurrentWeek++;

            UpdateTimestamp();

            return new CloseWeekResult
            {
                PaidOut = true,
                Owes = new Dictionary<Guid, decimal>(),
                Payout = payout,
                Week = targetWeek
            };
        }

        /// <summary>
        /// Get a report for the given week (defaults to CurrentWeek).
        /// </summary>
        public WeekReport GetReport(int? week = null)
        {
            var targetWeek = week ?? CurrentWeek;
            _paymentsByWeek.TryGetValue(targetWeek, out var weekPayments);
            weekPayments ??= new Dictionary<Guid, decimal>();

            var paidInFull = new List<Guid>();
            var owes = new Dictionary<Guid, decimal>();
            foreach (var m in Members)
            {
                weekPayments.TryGetValue(m.Id, out var paid);
                var owed = WeeklyContribution - paid;
                if (owed <= 0)
                    paidInFull.Add(m.Id);
                else
                    owes[m.Id] = owed;
            }

            return new WeekReport
            {
                Week = targetWeek,
                PaidInFull = paidInFull,
                Owes = owes,
                PayoutHistory = new List<PayoutEntry>(PayoutHistory)
            };
        }

        public sealed class PayoutEntry
        {
            public int Week { get; init; }
            public Guid RecipientId { get; init; }
            public string RecipientName { get; init; } = string.Empty;
            public decimal Amount { get; init; }
            public DateTime PaidAt { get; init; }
        }

        public sealed class CloseWeekResult
        {
            public int Week { get; init; }
            public bool PaidOut { get; init; }
            public Dictionary<Guid, decimal> Owes { get; init; } = new Dictionary<Guid, decimal>();
            public PayoutEntry? Payout { get; init; }
        }

        public sealed class WeekReport
        {
            public int Week { get; init; }
            public List<Guid> PaidInFull { get; init; } = new List<Guid>();
            public Dictionary<Guid, decimal> Owes { get; init; } = new Dictionary<Guid, decimal>();
            public List<PayoutEntry> PayoutHistory { get; init; } = new List<PayoutEntry>();
        }
    }
}
