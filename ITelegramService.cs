using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotTgBot
{
    public interface ITelegramService
    {
        Task StartAsync(CancellationToken ct);
        Task SendMessageAsync(long chatId, string text, CancellationToken ct = default);
    }
}
