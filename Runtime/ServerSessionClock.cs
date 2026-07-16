using System;
using ActionFit.Time;

namespace ActionFit.Time.Server
{
    /// <summary>Advances an in-memory server UTC baseline using only monotonic elapsed milliseconds.</summary>
    public sealed class ServerSessionClock : IClock
    {
        private readonly IMonotonicTimeSource _monotonicTimeSource;
        private long _serverUnixMsAtSync;
        private long _monotonicMsAtSync;
        private bool _isSynchronized;
        private bool _isPaused;

        public ServerSessionClock(IMonotonicTimeSource monotonicTimeSource)
        {
            _monotonicTimeSource = monotonicTimeSource
                                   ?? throw new ArgumentNullException(nameof(monotonicTimeSource));
        }

        public bool IsSynchronized => _isSynchronized;
        public bool IsPaused => _isPaused;

        public long CurrentUnixMilliseconds
        {
            get
            {
                if (!_isSynchronized)
                    throw new InvalidOperationException("Server time has not been synchronized.");

                if (_isPaused) return _serverUnixMsAtSync;

                long elapsedMilliseconds = _monotonicTimeSource.ElapsedMilliseconds - _monotonicMsAtSync;
                if (elapsedMilliseconds < 0L)
                    throw new InvalidOperationException("Monotonic time moved backwards.");

                return checked(_serverUnixMsAtSync + elapsedMilliseconds);
            }
        }

        public DateTime UtcNow =>
            DateTimeOffset.FromUnixTimeMilliseconds(CurrentUnixMilliseconds).UtcDateTime;

        /// <summary>Applies a fresh server response baseline and half of its measured round trip.</summary>
        public void Synchronize(long serverUnixMilliseconds, TimeSpan roundTripDuration)
        {
            if (roundTripDuration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(roundTripDuration));

            long halfRoundTripMilliseconds = checked((long)Math.Round(
                roundTripDuration.TotalMilliseconds / 2d,
                MidpointRounding.AwayFromZero));

            _serverUnixMsAtSync = checked(serverUnixMilliseconds + halfRoundTripMilliseconds);
            _monotonicMsAtSync = _monotonicTimeSource.ElapsedMilliseconds;
            _isSynchronized = true;
            _isPaused = false;
        }

        /// <summary>Freezes the last trusted session value until a later synchronization succeeds.</summary>
        public void Pause()
        {
            if (!_isSynchronized || _isPaused) return;

            _serverUnixMsAtSync = CurrentUnixMilliseconds;
            _monotonicMsAtSync = _monotonicTimeSource.ElapsedMilliseconds;
            _isPaused = true;
        }
    }
}
