using System;
using System.Threading.Tasks;
using ServerlessBlog.Engine.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Microsoft.Azure.Functions.Worker;

namespace ServerlessBlog.Engine
{
    public class TimerTrigger
    {
        private readonly ILogger<TimerTrigger> _logger;

        private readonly MetricService _metricService;

        public TimerTrigger(ILoggerFactory loggerFactory, MetricService metricService)
        {
            _logger = loggerFactory.CreateLogger<TimerTrigger>();
            _metricService = metricService;
        }

        [Function(nameof(CleanUpMetrics))]
        public async Task CleanUpMetrics([TimerTrigger("0 0 * * SUN")] TimerInfo myTimer)
        {
            _logger.LogInformation($"Timer Trigger {nameof(CleanUpMetrics)} executed at: {DateTime.Now}");
            int deletionDays = Convert.ToInt32(Environment.GetEnvironmentVariable("DeletionDays"), new CultureInfo("de-de"));
            await _metricService.CleanOldMetricsAsync(deletionDays);

            _logger.LogInformation($"Next execution at {myTimer.ScheduleStatus.Next}");
        }
    }
}
