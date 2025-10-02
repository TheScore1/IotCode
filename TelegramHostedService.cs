using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace IotTgBot
{
    public class TelegramHostedService : BackgroundService
    {
        private readonly ITelegramService _tg;
        public TelegramHostedService(ITelegramService tgServ)
        {
            _tg = tgServ;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _tg.StartAsync(stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Если у TelegramService есть StopAsync — вызовите здесь
            await base.StopAsync(cancellationToken);
        }
    }
}
