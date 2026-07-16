using System.Diagnostics;

namespace ActionFit.Time.Server
{
    /// <summary>Provides elapsed milliseconds without reading the device wall clock.</summary>
    public interface IMonotonicTimeSource
    {
        long ElapsedMilliseconds { get; }
    }

    /// <summary>Provides process-local monotonic elapsed milliseconds.</summary>
    public sealed class StopwatchMonotonicTimeSource : IMonotonicTimeSource
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
    }
}
