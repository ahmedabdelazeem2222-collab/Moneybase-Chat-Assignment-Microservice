using MoneyBase.Support.Application.DTOs;

namespace MoneyBase.Support.Application.Services
{
    public class ShiftService
    {
        #region Methods
        // Egypt timezone id (Windows: "Egypt Standard Time", Linux: "Africa/Cairo")
        private static readonly string[] CairoIds = new[] { "Egypt Standard Time", "Africa/Cairo" };
        private static TimeZoneInfo CairoTz => TimeZoneInfo.GetSystemTimeZones()
            .FirstOrDefault(t => CairoIds.Contains(t.Id)) ?? TimeZoneInfo.Utc;

        public DateTime GetCairoNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CairoTz);

        public int CalculateAgentCapacity(IEnumerable<AgentDto> agents)
        {
            var nowLocal = GetCairoNow();
            return agents.Where(a => a.IsOnShift(nowLocal)).Sum(a => a.MaxConcurrency);
        }

        public bool IsOfficeHours()
        {
            var h = GetCairoNow().Hour;
            return h >= 9 && h < 17;
        }

        #endregion
    }
}
