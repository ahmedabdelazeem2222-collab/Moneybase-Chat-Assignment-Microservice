using MoneyBase.Support.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyBase.Support.Application.DTOs
{
    public class ChatSessionDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public int UpdatedBy { get; set; }
        public ChatStatusEnum ChatStatus { get; set; }
        public DateTime LastPollAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime AssignedAt { get; set; }
        public Guid AgentId { get; set; }
    }
}
