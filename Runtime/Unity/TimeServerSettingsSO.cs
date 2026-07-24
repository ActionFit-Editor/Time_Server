using System;
using ActionFit.SOSingleton;
using ActionFit.Time;
using UnityEngine;

public enum TimeServerCalendarMode
{
    [InspectorName("UTC 00:00")]
    Utc = 0,

    [InspectorName("Device Local")]
    DeviceLocal = 1
}

/// <summary>프로젝트의 서버 시간과 게임 날짜 경계 정책을 보존하는 전역 설정입니다.</summary>
[CreateAssetMenu(fileName = "TimeServerSettingsSO", menuName = "Scriptable Objects/TimeServerSettingsSO")]
[ActionFitSettingsAsset("TimeServer", ActionFitSettingsAssetLifetime.RuntimeSingleton)]
public sealed class TimeServerSettingsSO : SO_Singleton<TimeServerSettingsSO>
{
    [SerializeField] private bool useServerTime;
    [SerializeField] private TimeServerCalendarMode calendarMode;
    [SerializeField, Range(-23, 23)] private int additionalOffsetHours;

    public bool UseServerTime => useServerTime;
    public TimeServerCalendarMode CalendarMode => calendarMode;
    public int DayBoundaryOffsetHours => additionalOffsetHours;

    [Obsolete("Use DayBoundaryOffsetHours.")]
    public int AdditionalOffsetHours => DayBoundaryOffsetHours;

    public TimeZoneInfo ContentCalendarTimeZone =>
        useServerTime && calendarMode == TimeServerCalendarMode.Utc
            ? TimeZoneInfo.Utc
            : TimeZoneInfo.Local;

    public TimeSpan ContentCalendarDayBoundaryOffset =>
        useServerTime
            ? TimeSpan.FromHours(DayBoundaryOffsetHours)
            : TimeSpan.Zero;

    /// <summary>설정된 device/server 시계와 달력 정책을 하나의 게임 시간 계약으로 결합합니다.</summary>
    public ConfiguredGameTime CreateGameTime(IClock deviceClock, IClock serverClock)
    {
        IClock selectedClock = useServerTime
            ? serverClock ?? throw new ArgumentNullException(nameof(serverClock))
            : deviceClock ?? throw new ArgumentNullException(nameof(deviceClock));

        return new ConfiguredGameTime(
            selectedClock,
            ContentCalendarTimeZone,
            ContentCalendarDayBoundaryOffset);
    }
}
