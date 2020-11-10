using System.Threading;
using System.Threading.Tasks;
using DynamicDns.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DynamicDns
{
    public class Worker : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IIPService _ipService;
        private readonly IDnsService _dnsService;
        private readonly ILogger<Worker> _logger;
        private string[] _domains;
        private string _ip;

        public Worker(IConfiguration configuration, IIPService ipService, IDnsService dnsService, ILogger<Worker> logger)
        {
            _configuration = configuration;
            _ipService = ipService;
            _dnsService = dnsService;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _domains = _configuration["DOMAINS"].Split(';');
            _ip = "";

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_ip != "")
                {
                    var newIP = await _ipService.GetIPAsync();

                    if (_ip != newIP)
                    {
                        foreach (var domain in _domains)
                        {
                            await _dnsService.CreateOrUpdate(_ip, domain);
                        }
                    }
                }
                else
                {
                    _ip = await _ipService.GetIPAsync();

                    foreach (var domain in _domains)
                    {
                        await _dnsService.CreateOrUpdate(_ip, domain);
                    }
                }

                _logger.LogInformation("Sleeping for 5 minutes");
                await Task.Delay(1000 * 60 * 5, stoppingToken);
            }
        }
    }
}
