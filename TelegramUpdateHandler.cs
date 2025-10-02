using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IotTgBot;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace IotTgBot
{
    public class TelegramUpdateHandler
    {
        private readonly IServiceProvider _services;
        private readonly IPlotService _plotService;

        public TelegramUpdateHandler(IServiceProvider services, IPlotService plotService)
        {
            _services = services;
            _plotService = plotService;
        }

        public async Task TelegramUpdateHandlerAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text is null) return;

            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text.Trim();

            await botClient.SendChatAction(chatId: chatId, action: ChatAction.Typing, cancellationToken: cancellationToken);

            if (text.StartsWith("/status") || text.StartsWith("/start"))
            {
                // создаём scope и получаем scoped репозиторий
                using var scope = _services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IReadingRepository>();

                var latest = await repo.GetLatestAsync(cancellationToken);
                if (latest == null)
                {
                    await botClient.SendMessage(chatId, "Нет данных.", cancellationToken: cancellationToken);
                }
                else
                {
                    string msg = $"Последние данные (UTC {latest.TimestampUtc:u}):\n" +
                                 $"T = {latest.TemperatureC:F2} °C\n" +
                                 $"H = {latest.HumidityPct:F2} %\n" +
                                 $"P = {latest.PressureHpa:F2} hPa";
                    await botClient.SendMessage(chatId, msg, cancellationToken: cancellationToken);
                }
                return;
            }

            if (text.StartsWith("/plot"))
            {
                await botClient.SendChatAction(chatId, ChatAction.UploadPhoto);

                // ожид. формат: /plot hour  или /plot day
                var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (!parts[0].Equals("/plot", StringComparison.OrdinalIgnoreCase)) return;
                var range = parts.Length > 1 ? parts[1].ToLowerInvariant() : "hour";

                DateTime toUtc = DateTime.UtcNow;
                DateTime fromUtc = range switch
                {
                    "hour" => toUtc.AddHours(-1),
                    "day" => toUtc.AddDays(-1),
                    "month" => toUtc.AddMonths(-1),
                    "year" => toUtc.AddYears(-1),
                    _ => toUtc.AddHours(-1)
                };

                using var scope = _services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IReadingRepository>();

                var data = await repo.GetRangeAsync(fromUtc, toUtc, cancellationToken);

                if (data.Count == 0)
                {
                    await botClient.SendMessage(chatId, "Нет данных за выбранный период.", cancellationToken: cancellationToken);
                    return;
                }

                // parts: /plot <range> <metric?>
                // metric: "temp"|"hum"|"pres"|"combined"|"all"(=separate)
                // после получения data, title, etc.
                var metricArg = parts.Length > 2 ? parts[2].ToLowerInvariant() : "all";

                if (metricArg == "all" || metricArg == "separate")
                {
                    var list = new (string key, string caption)[] { ("temp", "Temperature (°C)"), ("hum", "Humidity (%)"), ("pres", "Pressure (hPa)") };
                    foreach (var (key, caption) in list)
                    {
                        var t = $"{caption} за {range} (from {fromUtc:u} to {toUtc:u})";
                        byte[] png;
                        if (key == "pres")
                            png = await _plotService.PlotMetricAsync(data, t, "pressure", cancellationToken);
                        else if (key == "hum")
                            png = await _plotService.PlotMetricAsync(data, t, "humidity", cancellationToken);
                        else
                            png = await _plotService.PlotMetricAsync(data, t, "temperature", cancellationToken);

                        using var ms2 = new MemoryStream(png);
                        ms2.Position = 0;
                        var input = InputFile.FromStream(ms2, $"{key}.png");
                        await botClient.SendPhoto(chatId, input, caption: caption, cancellationToken: cancellationToken);
                        await Task.Delay(300, cancellationToken); // небольшая пауза
                    }
                }
                else if (metricArg == "combined")
                {
                    var combinedTitle = $"Combined: {range} (from {fromUtc:u} to {toUtc:u})";
                    var png = await _plotService.PlotCombinedAsync(data, combinedTitle, cancellationToken);
                    using var ms2 = new MemoryStream(png);
                    ms2.Position = 0;
                    await botClient.SendPhoto(chatId, InputFile.FromStream(ms2, "combined.png"), caption: "Combined T/H/P", cancellationToken: cancellationToken);
                }
                else
                {
                    // single metric
                    string metricKey = metricArg switch
                    {
                        "hum" or "humidity" => "humidity",
                        "pres" or "pressure" => "pressure",
                        _ => "temperature",
                    };
                    var t = $"{metricKey} за {range} (from {fromUtc:u} to {toUtc:u})";
                    var png = await _plotService.PlotMetricAsync(data, t, metricKey, cancellationToken);
                    using var ms2 = new MemoryStream(png);
                    ms2.Position = 0;
                    await botClient.SendPhoto(chatId, InputFile.FromStream(ms2, $"{metricKey}.png"), caption: $"График: {metricKey}", cancellationToken: cancellationToken);
                }

            }

            // по умолчанию — эхо / помощь
            if (text.StartsWith("/help"))
            {
                await botClient.SendMessage(chatId, "/status - последние данные\n/plot hour|day|month|year|all - график", cancellationToken: cancellationToken);
                return;
            }
        }

        public Task TelegramErrorHandlerAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[Telegram Polling Error] {exception}");
            return Task.CompletedTask;
        }
    }
}
