using MoneyBase.Support.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyBase.Support.Application.Interfaces
{
    public interface IChatAssignmentService
    {
        Task AssignChatAsync(ChatRequestDto chat, CancellationToken stoppingToken);
    }
}
