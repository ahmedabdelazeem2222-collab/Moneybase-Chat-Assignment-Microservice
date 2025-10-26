using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MoneyBase.Support.Application.DTOs
{
    public class ChatRequestDto
    {
        [Required(ErrorMessage = "ChatId is required")]
        [MaxLength(50, ErrorMessage = "ChatId max length 50")]
        public Guid ChatId { get; set; }

        [Required(ErrorMessage = "User is required")]
        public string UserId { get; set; }

        [JsonIgnore]
        public DateTime CreatedDate { get; set; }
        public ChatRequestDto() { }
        public ChatRequestDto(Guid chatId, string userId, DateTime createdDate)
        {
            ChatId = chatId;
            UserId = userId;
            CreatedDate = createdDate;
        }
    }
}
