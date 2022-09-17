using System;
using System.Threading.Tasks;
using Engine.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Engine.Trigger
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

        [FunctionName(nameof(CleanUpMetrics))]
        public async Task CleanUpMetrics([TimerTrigger("0 0 * * SUN")]TimerInfo myTimer)
        {
            _logger.LogInformation($"Timer Trigger {nameof(CleanUpMetrics)} executed at: {DateTime.Now}");
            int deletionDays = Convert.ToInt16(Environment.GetEnvironmentVariable("DeletionDays"));
            await _metricService.CleanOldMetricsAsync(deletionDays);
        }
    }
}
