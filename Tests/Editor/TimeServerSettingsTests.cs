using System;
using System.Reflection;
using ActionFit.SOSingleton;
using ActionFit.SOSingleton.Editor;
using ActionFit.Time;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ActionFit.Time.Server.Tests
{
    public sealed class TimeServerSettingsTests
    {
        private TimeServerSettingsSO _settings;
        private SerializedObject _serializedSettings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<TimeServerSettingsSO>();
            _serializedSettings = new SerializedObject(_settings);
        }

        [TearDown]
        public void TearDown()
        {
            _serializedSettings.Dispose();
            UnityEngine.Object.DestroyImmediate(_settings);
        }

        [Test]
        public void ServerDisabled_SelectsDeviceLocalWithZeroBoundary()
        {
            Configure(false, TimeServerCalendarMode.Utc, -9);
            var deviceClock = new FixedClock(Utc(2026, 7, 15, 1, 0));

            ConfiguredGameTime gameTime = _settings.CreateGameTime(deviceClock, null);

            Assert.That(gameTime.UtcNow, Is.EqualTo(deviceClock.UtcNow));
            Assert.That(gameTime.CalendarTimeZone.Id, Is.EqualTo(TimeZoneInfo.Local.Id));
            Assert.That(gameTime.DayBoundaryOffset, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void ServerEnabled_UtcAppliesSignedBoundaryWithoutChangingUtc()
        {
            Configure(true, TimeServerCalendarMode.Utc, -9);
            var serverClock = new FixedClock(Utc(2026, 7, 15, 15, 0));

            ConfiguredGameTime gameTime = _settings.CreateGameTime(null, serverClock);

            Assert.That(gameTime.UtcNow, Is.EqualTo(serverClock.UtcNow));
            Assert.That(gameTime.CalendarTimeZone, Is.SameAs(TimeZoneInfo.Utc));
            Assert.That(gameTime.DayBoundaryOffset, Is.EqualTo(TimeSpan.FromHours(-9)));
            Assert.That(gameTime.Now, Is.EqualTo(new DateTime(2026, 7, 16, 0, 0, 0)));
        }

        [TestCase(-9)]
        [TestCase(9)]
        public void ServerEnabled_DeviceLocalAppliesSignedBoundary(int hours)
        {
            Configure(true, TimeServerCalendarMode.DeviceLocal, hours);
            var serverClock = new FixedClock(Utc(2026, 7, 15, 15, 0));

            ConfiguredGameTime gameTime = _settings.CreateGameTime(null, serverClock);

            Assert.That(gameTime.CalendarTimeZone.Id, Is.EqualTo(TimeZoneInfo.Local.Id));
            Assert.That(gameTime.DayBoundaryOffset, Is.EqualTo(TimeSpan.FromHours(hours)));
        }

        [Test]
        public void ServerEnabled_UtcZeroChangesDateAtUtcMidnight()
        {
            Configure(true, TimeServerCalendarMode.Utc, 0);
            var serverClock = new ManualClock(Utc(2026, 7, 15, 23, 59));

            ConfiguredGameTime gameTime = _settings.CreateGameTime(null, serverClock);
            Assert.That(gameTime.Today, Is.EqualTo(new DateTime(2026, 7, 15)));

            serverClock.Advance(TimeSpan.FromMinutes(1));

            Assert.That(gameTime.Now, Is.EqualTo(new DateTime(2026, 7, 16, 0, 0, 0)));
            Assert.That(gameTime.Today, Is.EqualTo(new DateTime(2026, 7, 16)));
        }

        [Test]
        public void CreateGameTime_RequiresOnlyTheSelectedClock()
        {
            Configure(false, TimeServerCalendarMode.Utc, 0);
            Assert.Throws<ArgumentNullException>(() => _settings.CreateGameTime(null, null));

            Configure(true, TimeServerCalendarMode.Utc, 0);
            Assert.Throws<ArgumentNullException>(() => _settings.CreateGameTime(null, null));
        }

        [Test]
        public void LegacyAdditionalOffsetProperty_ReturnsSignedBoundaryValue()
        {
            Configure(true, TimeServerCalendarMode.Utc, -9);

#pragma warning disable CS0618
            int legacyValue = _settings.AdditionalOffsetHours;
#pragma warning restore CS0618

            Assert.That(legacyValue, Is.EqualTo(-9));
            Assert.That(_settings.DayBoundaryOffsetHours, Is.EqualTo(-9));
        }

        [Test]
        public void SerializedCompatibility_PreservesAdditionalOffsetHoursKeyAndValue()
        {
            Configure(true, TimeServerCalendarMode.Utc, -9);
            string json = EditorJsonUtility.ToJson(_settings);
            var restored = ScriptableObject.CreateInstance<TimeServerSettingsSO>();

            try
            {
                EditorJsonUtility.FromJsonOverwrite(json, restored);

                Assert.That(json, Does.Contain("\"additionalOffsetHours\":-9"));
                Assert.That(restored.DayBoundaryOffsetHours, Is.EqualTo(-9));
                Assert.That(restored.CalendarMode, Is.EqualTo(TimeServerCalendarMode.Utc));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored);
            }
        }

        [Test]
        public void Registration_DeclaresRuntimeSingletonOwner()
        {
            ActionFitSettingsAssetAttribute registration =
                typeof(TimeServerSettingsSO).GetCustomAttribute<ActionFitSettingsAssetAttribute>();

            Assert.That(registration, Is.Not.Null);
            Assert.That(registration.Owner, Is.EqualTo("TimeServer"));
            Assert.That(
                registration.Lifetime,
                Is.EqualTo(ActionFitSettingsAssetLifetime.RuntimeSingleton));
            Assert.That((int)TimeServerCalendarMode.Utc, Is.EqualTo(0));
            Assert.That((int)TimeServerCalendarMode.DeviceLocal, Is.EqualTo(1));
        }

        private void Configure(
            bool useServerTime,
            TimeServerCalendarMode calendarMode,
            int dayBoundaryOffsetHours)
        {
            _serializedSettings.FindProperty("useServerTime").boolValue = useServerTime;
            _serializedSettings.FindProperty("calendarMode").enumValueIndex = (int)calendarMode;
            _serializedSettings.FindProperty("additionalOffsetHours").intValue =
                dayBoundaryOffsetHours;
            _serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static DateTime Utc(int year, int month, int day, int hour, int minute)
        {
            return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        }
    }

    public sealed class TimeServerSettingsAssetLifecycleTests
    {
        private const string TestRoot = "Assets/_Data/_MCC1643TimeServerTests";
        private static readonly MethodInfo ResolveForTests = typeof(ActionFitSettingsAssetProvider)
            .GetMethod("ResolveForTests", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            ActionFitSettingsAssetProvider.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            ActionFitSettingsAssetProvider.ClearCache();
            AssetDatabase.DeleteAsset(TestRoot);
        }

        [Test]
        public void Resolve_MissingAssetCreatesOnlyAtCanonicalPath()
        {
            ActionFitSettingsAssetAttribute registration = CreateRegistration();

            ActionFitSettingsAssetResolution result = Resolve(registration, true);

            Assert.That(result.Status, Is.EqualTo(ActionFitSettingsAssetStatus.Created));
            Assert.That(result.ActualPath, Is.EqualTo(registration.CanonicalPath));
            Assert.That(AssetDatabase.LoadMainAssetAtPath(registration.CanonicalPath), Is.Not.Null);
        }

        [Test]
        public void Resolve_UniqueExistingAssetReusesGuidAndPath()
        {
            const string existingPath = TestRoot + "/Existing.asset";
            TimeServerSettingsLifecycleFixture existing =
                CreateAsset<TimeServerSettingsLifecycleFixture>(existingPath);
            string guid = AssetDatabase.AssetPathToGUID(existingPath);
            ActionFitSettingsAssetProvider.ClearCache();

            ActionFitSettingsAssetResolution result = Resolve(CreateRegistration(), true);

            Assert.That(result.Status, Is.EqualTo(ActionFitSettingsAssetStatus.FoundUnique));
            Assert.That(result.Asset, Is.SameAs(existing));
            Assert.That(result.ActualPath, Is.EqualTo(existingPath));
            Assert.That(AssetDatabase.AssetPathToGUID(existingPath), Is.EqualTo(guid));
        }

        [Test]
        public void Resolve_DuplicateAssetsBlockWithoutCreatingCanonicalAsset()
        {
            CreateAsset<TimeServerSettingsLifecycleFixture>(TestRoot + "/DuplicateA.asset");
            CreateAsset<TimeServerSettingsLifecycleFixture>(TestRoot + "/DuplicateB.asset");
            ActionFitSettingsAssetAttribute registration = CreateRegistration();
            ActionFitSettingsAssetProvider.ClearCache();

            ActionFitSettingsAssetResolution result = Resolve(registration, true);

            Assert.That(result.Status, Is.EqualTo(ActionFitSettingsAssetStatus.Duplicate));
            Assert.That(result.Asset, Is.Null);
            Assert.That(AssetDatabase.LoadMainAssetAtPath(registration.CanonicalPath), Is.Null);
        }

        private static ActionFitSettingsAssetAttribute CreateRegistration()
        {
            return new ActionFitSettingsAssetAttribute(
                "MCC1643TimeServerTests",
                ActionFitSettingsAssetLifetime.RuntimeSingleton)
            {
                CanonicalPath =
                    TestRoot + "/Canonical/Resources/SO/TimeServerSettingsLifecycleFixture.asset"
            };
        }

        private static ActionFitSettingsAssetResolution Resolve(
            ActionFitSettingsAssetAttribute registration,
            bool createIfMissing)
        {
            Assert.That(ResolveForTests, Is.Not.Null);
            return (ActionFitSettingsAssetResolution)ResolveForTests.Invoke(
                null,
                new object[]
                {
                    typeof(TimeServerSettingsLifecycleFixture),
                    registration,
                    createIfMissing
                });
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            EnsureFolder(path.Substring(0, path.LastIndexOf('/')));
            T instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, path);
            return instance;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }

}
