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
        public int CurrentWeek { get; set; } = 1;

        // Index of next member to receive the pot (round-robin)
        public int NextPayoutIndex { get; set; } = 0;

        public static Group Create(string name, string description, decimal weeklyContribution)
        {
            if (weeklyContribution <= 0)
                throw new ArgumentException("Weekly contribution must be positive.", nameof(weeklyContribution));

            var group = new Group
            {
                Name = name,
                Description = description,
                WeeklyContribution = weeklyContribution
            };

            group.MarkDateCreated();
            return group;
        }

        public void AddMember(Member member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            foreach (var m in Members)
            {
                if (m.Id == member.Id) throw new InvalidOperationException("Member with the same Id already exists.");
                if (string.Equals(m.Name, member.Name, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Member with the same name already exists.");
            }
            Members.Add(member);
            member.AssignToGroup(this);
            UpdateTimestamp();
        }

        public void RemoveMember(Member member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            Members.Remove(member);
            UpdateTimestamp();
        }

        public void UpdateGroupDetails(string name, string description)
        {
            Name = name;
            Description = description;
            UpdateTimestamp();
        }
        // Reporting and closing logic moved to application layer service.
    }
}
