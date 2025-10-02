using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IotTgBot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace IotTgBot
{
    public class TelegramService : ITelegramService
    {
        private readonly TelegramBotClient _client;
        private readonly BotOptions _opts;
        private readonly TelegramUpdateHandler _updateHandler;
        private readonly ILogger<TelegramService> _logger;

        public TelegramService(IOptions<BotOptions> opts,
            TelegramUpdateHandler updateHandler,
            ILogger<TelegramService> logger)
        {
            _opts = opts.Value;
            _logger = logger;
            _updateHandler = updateHandler;
            var httpClient = new HttpClient
            {
                // Например, 5 минут — этого хватит, чтобы Request не обрывался при long‑polling
                Timeout = TimeSpan.FromMinutes(5)
            };
            _client = new TelegramBotClient(_opts.TelegramToken, httpClient);
        }

        public async Task SendMessageAsync(long chatId, string text, CancellationToken ct = default)
        {
            await _client.SendMessage(chatId: chatId, text: text, cancellationToken: ct);
        }

        public async Task StartAsync(CancellationToken ct)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            // В v22.5.1 остаётся только этот вариант
            _client.StartReceiving(
                updateHandler: new DefaultUpdateHandler(
                    _updateHandler.TelegramUpdateHandlerAsync,
                    _updateHandler.TelegramErrorHandlerAsync),
                receiverOptions: receiverOptions,
                cancellationToken: ct
            );

            Console.WriteLine("[ INFO ][Telegram] Бот запущен.");
        }
    }
}
