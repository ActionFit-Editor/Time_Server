# ActionFit Time Server (`com.actionfit.time.server`)

캐시되지 않은 HTTPS `Date` 응답을 세션 기준 UTC로 동기화하고, 이후 시간을 기기 wall clock이 아닌 monotonic elapsed time으로 진행합니다.

## 주요 기능

- `ConnectivityObservation`의 fresh `Date`와 `Age` 검증 결과 사용
- 표준 HTTP `Date`를 Unix milliseconds로 즉시 변환
- `Date + roundTrip/2` 기준값 적용
- `ServerSessionClock`의 monotonic 진행과 pause freeze
- 동기화 전 또는 실패 상태에서 기기 시간 fallback 없이 fail-closed 처리
- 동기화된 기준값을 메모리에만 유지

## 기본 사용법

```csharp
using System;
using ActionFit.Connectivity;
using ActionFit.Time.Server;

var clock = new ServerSessionClock(new StopwatchMonotonicTimeSource());
var synchronizer = new ServerTimeSynchronizer(
    new UnityWebRequestConnectivityProbe(),
    clock);

ServerTimeSynchronizationResult result = await synchronizer.SynchronizeAsync(
    new Uri("https://example.com/connectivity"),
    TimeSpan.FromSeconds(5));
```

`ServerTimeSynchronizer`는 숨은 재시도를 수행하지 않습니다. 시작 차단, 재시도 간격, 포그라운드 복구, UI 표시는 프로젝트 어댑터가 담당합니다.

## 시간 계약

- 내부 기준값은 Unix milliseconds입니다.
- 현재 시각은 `serverUnixMsAtSync + monotonicElapsedMs`로 계산합니다.
- `DateTime` 변환은 기존 `IClock.UtcNow` 호환 경계에서만 수행합니다.
- `Pause()` 이후에는 새 동기화가 성공할 때까지 마지막 신뢰 시각을 고정합니다.
- `DateTime.Now`, `DateTime.UtcNow`, `SystemClock.Instance.UtcNow`를 서버 시계 경로에서 사용하지 않습니다.

## 패키지 경계

이 패키지는 서버 UTC 확보와 세션 시계만 소유합니다. 프로젝트 설정 SO, endpoint 선택, 시작/포그라운드 gate, 게임 입력 제한, 일일 이벤트 catch-up 및 DevTool offset은 consuming project가 소유합니다.

현재 패키지와 의존 패키지는 Private 배포 대상입니다. 게시, 원격 저장소 생성, tag 및 catalog 등록은 Custom Package Manager의 수동 게시 흐름에서 별도로 수행합니다.

## 설치

현재 Cat Merge Cafe에서는 embedded package로 사용합니다. Custom Package Manager를 통해 Private 저장소와 `1.0.0` tag를 수동 게시한 뒤 다른 인증된 프로젝트에서는 다음 Git UPM 주소를 사용할 수 있습니다.

```json
{
  "dependencies": {
    "com.actionfit.time.server": "https://github.com/ActionFitGames/Time_Server.git#1.0.0"
  }
}
```

## Unity Menu

- Package root: `Tools > Package > ActionFit Time Server`
- README: `Tools > Package > ActionFit Time Server > README`

## 테스트

Unity Test Framework의 EditMode에서 `com.actionfit.time.server.Editor.Tests`를 실행해 Unix milliseconds 계산, half-RTT, pause freeze, fresh/stale 관측값과 fail-closed 상태를 검증할 수 있습니다.
