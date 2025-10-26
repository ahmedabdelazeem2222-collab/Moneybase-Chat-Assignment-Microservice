using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyBase.Support.Application.DTOs
{
    public class ChatAssignedMessage
    {
        public Guid ChatId { get; set; }
        public Guid AgentId { get; set; }
        public string UserId { get; set; }
        public string Channel { get; set; } // e.g., web, mobile, etc.
        public DateTime AssignedAt { get; set; }
        public string Priority { get; set; } // optional
    }
}
