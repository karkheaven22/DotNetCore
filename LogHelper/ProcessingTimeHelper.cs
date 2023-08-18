using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LogHelper
{
    public enum MeasureOn
    {
        Database,
        ExternalService,
        OAuth
    }

    public interface IProcessingTime
    {
        void StartLogging(MeasureOn measureOn, string actionName, string objectName);

        void StopLogging();
    }

    public class ProcessingTimeHelper : IProcessingTime
    {
        private readonly Stopwatch _stopwatch = new();
        private string? _actionName;
        private string? _objectName;
        private MeasureOn _measureOn;
        private readonly ILogger _logger;

        public ProcessingTimeHelper(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("ProcessingTimeHelper");
        }

        public void StartLogging(MeasureOn measureOn, string actionName, string objectName)
        {
            _stopwatch.Reset();

            _measureOn = measureOn;
            _actionName = actionName;
            _objectName = objectName;

            _stopwatch.Start();
        }

        public void StopLogging()
        {
            _stopwatch.Stop();
            _logger.LogWarning("Processing time: {MeasureOn} {ActionName} {ObjectName} {ElapsedTime:0.0000} seconds",
                               _measureOn.ToString(),
                               _actionName,
                               _objectName,
                               _stopwatch.Elapsed.TotalSeconds);
        }
    }
}