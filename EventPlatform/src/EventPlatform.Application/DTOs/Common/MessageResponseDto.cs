using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatform.Application.DTOs.Common
{
    public class MessageResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public MessageResponseDto(string msg) => Message = msg;
    }
}
