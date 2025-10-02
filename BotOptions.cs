using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IotTgBot
{
    public class BotOptions
    {
        public string TelegramToken { get; set; } = "";
        public char CommandPrefix { get; set; } = '.';
        public List<long> AdminsTelegramChatId { get; set; } = new List<long>();
    }
}
