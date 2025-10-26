using MoneyBase.Support.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyBase.Support.Application.DTOs
{
    public class AgentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public SeniorityEnum Seniority { get; set; }
        public int ShiftStartHour { get; set; }
        public int ShiftEndHour { get; set; }
        public bool IsOverflow { get; set; }
        public DateTime CreationDate { get; set; }
        public int CreatedBy { get; set; } = 0; // system
        public int? UpdatedBy { get; set; } = 0;
        public bool IsDeleted { get; set; }

        // Maximum number of simultaneous chats/tasks this agent can handle
        public int MaxConcurrency { get; set; }
        public IEnumerable<string> AssignedChatIds { get; set; } = new List<string>();
        // Checks if the agent is currently on shift for the given local time
        public bool IsOnShift(DateTime localTime)
        {
            int hour = localTime.Hour;

            if (ShiftStartHour <= ShiftEndHour)
            {
                // Normal shift within same day
                return hour >= ShiftStartHour && hour < ShiftEndHour;
            }
            else
            {
                // Overnight shift (e.g., 22:00 to 06:00)
                return hour >= ShiftStartHour || hour < ShiftEndHour;
            }
        }
    }
}
