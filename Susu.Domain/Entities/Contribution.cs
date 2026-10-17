using System;

namespace Susu.Domain.Entities
{
    public sealed class Contribution
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GroupId { get; init; }
        public Guid MemberId { get; init; }
        public int Week { get; init; }
        public decimal Amount { get; init; }
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    }
}
