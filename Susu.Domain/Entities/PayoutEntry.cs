using System;

namespace Susu.Domain.Entities
{
    public sealed class PayoutEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GroupId { get; init; }
        public int Week { get; init; }
        public Guid RecipientId { get; init; }
        public string RecipientName { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    }
}
