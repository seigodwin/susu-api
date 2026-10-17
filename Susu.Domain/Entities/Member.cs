using System;
using System.Collections.Generic;
using System.Text;

namespace Susu.Domain.Entities
{
    public sealed class Member : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public Guid? GroupId { get; private set; }
        public Group Group { get; private set; } = null!;
        public static Member Create(string name, string email, Guid? groupId)
        {
            var member = new Member
            {
                Name = name,
                Email = email,
                GroupId = groupId
            };

            member.MarkDateCreated();
            return member;
        }
        public void UpdateMemberDetails(string name, string email)
        {
            Name = name;
            Email = email;
            UpdateTimestamp();
        }

        public void AssignToGroup(Group group)
        {
            if(group == null)
                throw new ArgumentNullException(nameof(group));
            GroupId = group.Id;
            Group = group;
            UpdateTimestamp();
        }
    }
}
