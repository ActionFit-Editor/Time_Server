using System;
using System.Threading;
using System.Threading.Tasks;
using ActionFit.Connectivity;
using NUnit.Framework;

namespace ActionFit.Time.Server.Tests
{
    public class ServerTimeTests
    {
        [Test]
        public void UnsynchronizedClock_RejectsCurrentTimeRead()
        {
            var clock = new ServerSessionClock(new ManualMonotonicTimeSource());

            Assert.Throws<InvalidOperationException>(() => _ = clock.CurrentUnixMilliseconds);
            Assert.Throws<InvalidOperationException>(() => _ = clock.UtcNow);
        }

        [Test]
        public void SynchronizedClock_AddsHalfRoundTripAndMonotonicElapsedTime()
        {
            var monotonic = new ManualMonotonicTimeSource(1_000L);
            var clock = new ServerSessionClock(monotonic);
            long serverUnixMilliseconds = new DateTimeOffset(
                2026,
                7,
                15,
                12,
                34,
                56,
                TimeSpan.Zero).ToUnixTimeMilliseconds();

            clock.Synchronize(serverUnixMilliseconds, TimeSpan.FromMilliseconds(240));
            monotonic.Advance(1_500L);

            Assert.That(
                clock.CurrentUnixMilliseconds,
                Is.EqualTo(serverUnixMilliseconds + 120L + 1_500L));
            Assert.That(clock.UtcNow.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void PausedClock_FreezesUntilFreshSynchronization()
        {
            var monotonic = new ManualMonotonicTimeSource();
            var clock = new ServerSessionClock(monotonic);
            clock.Synchronize(1_000_000L, TimeSpan.Zero);
            monotonic.Advance(500L);

            clock.Pause();
            monotonic.Advance(10_000L);

            Assert.That(clock.CurrentUnixMilliseconds, Is.EqualTo(1_000_500L));
            Assert.That(clock.IsPaused, Is.True);

            clock.Synchronize(2_000_000L, TimeSpan.Zero);

            Assert.That(clock.CurrentUnixMilliseconds, Is.EqualTo(2_000_000L));
            Assert.That(clock.IsPaused, Is.False);
        }

        [Test]
        public async Task Synchronizer_FreshObservation_InitializesClockAndBypassesCache()
        {
            var monotonic = new ManualMonotonicTimeSource();
            var clock = new ServerSessionClock(monotonic);
            ConnectivityObservation observation = ConnectivityObservationParser.Parse(
                true,
                204L,
                "Wed, 15 Jul 2026 12:34:56 GMT",
                "0",
                TimeSpan.FromMilliseconds(200));
            var probe = new FakeObservationProbe(observation);
            var synchronizer = new ServerTimeSynchronizer(probe, clock);

            ServerTimeSynchronizationResult result = await synchronizer.SynchronizeAsync(
                new Uri("https://example.com/time"),
                TimeSpan.FromSeconds(3));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(clock.IsSynchronized, Is.True);
            Assert.That(probe.BypassCache, Is.True);
            Assert.That(
                clock.CurrentUnixMilliseconds,
                Is.EqualTo(observation.ServerDateUtc.Value.ToUnixTimeMilliseconds() + 100L));
        }

        [TestCase("5")]
        [TestCase("invalid")]
        public async Task Synchronizer_StaleOrInvalidAge_DoesNotInitializeClock(string ageHeader)
        {
            ConnectivityObservation observation = ConnectivityObservationParser.Parse(
                true,
                204L,
                "Wed, 15 Jul 2026 12:34:56 GMT",
                ageHeader,
                TimeSpan.Zero);
            var clock = new ServerSessionClock(new ManualMonotonicTimeSource());
            var synchronizer = new ServerTimeSynchronizer(
                new FakeObservationProbe(observation),
                clock);

            ServerTimeSynchronizationResult result = await synchronizer.SynchronizeAsync(
                new Uri("https://example.com/time"),
                TimeSpan.FromSeconds(3));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(clock.IsSynchronized, Is.False);
        }

        [Test]
        public void Synchronizer_RejectsNonHttpsEndpoint()
        {
            var synchronizer = new ServerTimeSynchronizer(
                new FakeObservationProbe(default),
                new ServerSessionClock(new ManualMonotonicTimeSource()));

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await synchronizer.SynchronizeAsync(
                    new Uri("http://example.com/time"),
                    TimeSpan.FromSeconds(3)));
        }

        private sealed class ManualMonotonicTimeSource : IMonotonicTimeSource
        {
            public ManualMonotonicTimeSource(long elapsedMilliseconds = 0L)
            {
                ElapsedMilliseconds = elapsedMilliseconds;
            }

            public long ElapsedMilliseconds { get; private set; }

            public void Advance(long milliseconds)
            {
                ElapsedMilliseconds = checked(ElapsedMilliseconds + milliseconds);
            }
        }

        private sealed class FakeObservationProbe : IConnectivityObservationProbe
        {
            private readonly ConnectivityObservation _observation;

            public FakeObservationProbe(ConnectivityObservation observation)
            {
                _observation = observation;
            }

            public bool BypassCache { get; private set; }

            public Task<ConnectivityObservation> ObserveAsync(
                Uri endpoint,
                TimeSpan timeout,
                bool bypassCache,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                BypassCache = bypassCache;
                return Task.FromResult(_observation);
            }
        }
    }
}
