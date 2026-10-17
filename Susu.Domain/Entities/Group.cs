using System;
using System.Collections.Generic;
using System.Text;

namespace Susu.Domain.Entities
{
    public sealed class Group : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public decimal ContributionAmount { get; private set; } = 0;
        public ICollection<Member> Members { get; private set; } = new List<Member>();
     

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
        
        public void AddMember(Member member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            Members.Add(member);
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
    }
}
