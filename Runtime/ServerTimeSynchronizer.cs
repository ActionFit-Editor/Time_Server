using System;
using System.Threading;
using System.Threading.Tasks;
using ActionFit.Connectivity;

namespace ActionFit.Time.Server
{
    public readonly struct ServerTimeSynchronizationResult
    {
        public ServerTimeSynchronizationResult(bool isSuccess, ConnectivityObservation observation)
        {
            IsSuccess = isSuccess;
            Observation = observation;
        }

        public bool IsSuccess { get; }
        public ConnectivityObservation Observation { get; }
    }

    /// <summary>Applies one fresh HTTPS Date observation to a session clock without hidden retries.</summary>
    public sealed class ServerTimeSynchronizer
    {
        private readonly IConnectivityObservationProbe _observationProbe;
        private readonly ServerSessionClock _clock;

        public ServerTimeSynchronizer(
            IConnectivityObservationProbe observationProbe,
            ServerSessionClock clock)
        {
            _observationProbe = observationProbe
                                ?? throw new ArgumentNullException(nameof(observationProbe));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Requests and applies one fresh HTTPS Date observation.</summary>
        public async Task<ServerTimeSynchronizationResult> SynchronizeAsync(
            Uri endpoint,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            if (endpoint.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Server time endpoint must use HTTPS.", nameof(endpoint));
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            ConnectivityObservation observation = await _observationProbe.ObserveAsync(
                endpoint,
                timeout,
                true,
                cancellationToken);

            if (!observation.HasFreshServerDate)
                return new ServerTimeSynchronizationResult(false, observation);

            long serverUnixMilliseconds = observation.ServerDateUtc.Value.ToUnixTimeMilliseconds();
            _clock.Synchronize(serverUnixMilliseconds, observation.RoundTripDuration);
            return new ServerTimeSynchronizationResult(true, observation);
        }
    }
}
