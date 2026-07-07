# Unity-Broadcast-Timer

Unity 기반 투네이션 룰렛 연동 타이머 프로젝트입니다.  
Toonation Alertbox 알림 데이터를 수신하고, 설정한 룰렛 결과에 따라 타이머 시간을 자동으로 추가하거나 차감할 수 있도록 구현했습니다.

외부 연동 없이 사용할 수 있는 일반 타이머 모드도 함께 지원합니다.

## 주요 기능

- 일반 타이머 모드 지원
- Toonation Alertbox URL을 통한 연동 모드 지원
- WebSocket 기반 알림 데이터 수신
- 설정한 룰렛 제목과 일치하는 결과만 타이머에 반영
- 룰렛 결과에 따른 시간 추가 / 차감 처리
- Delay 설정을 통한 결과 반영 시점 조절
- Save URL / Save Timer 설정 지원

## 실행 흐름

### 일반 모드

```text
프로그램 실행
→ 일반 모드 선택
→ TimerPanel 진입
→ 타이머 직접 조작
```

### 연동 모드

```text
프로그램 실행
→ 연동 모드 선택
→ Toonation Alertbox URL 입력
→ Connect
→ 연결 성공
→ TimerPanel 진입
→ 룰렛 결과 수신 시 타이머 자동 반영
```

## 룰렛 결과 적용 방식

설정창에서 타이머에 반영할 룰렛 제목을 입력할 수 있습니다.  
수신한 알림 메시지의 앞부분이 설정한 룰렛 제목과 일치할 때만 결과를 처리합니다.

```text
노방종 룰렛 - 5분 추가
→ 타이머에 5분 추가
```

```text
노방종 룰렛 - 10분 차감
→ 타이머에서 10분 차감
```

```text
다른 룰렛 - 5분 추가
→ 설정한 룰렛 제목과 일치하지 않으므로 무시
```

`추가`, `차감` 키워드를 기준으로 타이머 시간을 변경하며, `광`, `꽝`처럼 시간 변화가 없는 결과는 무시합니다.

## 화면 구성

- StartPanel
  - 일반 모드 / 연동 모드 선택

- SetupPanel
  - Toonation Alertbox URL 입력
  - Connect
  - 연동 상세 설정
  - 뒤로가기

- DetailSettingPanel
  - 룰렛 제목 설정
  - Delay 설정
  - Save URL 설정

- TimerPanel
  - 남은 시간 표시
  - 시간 추가 / 차감
  - 타이머 정지 / 재개

- TimerSettingPanel
  - Save Timer 설정
  - 처음 화면으로 돌아가기

## 사용 방법

자세한 사용 방법은 별도 문서에 정리했습니다.

[USAGE.md](./USAGE.md)

## 외부 패키지 / 참고 자료

- donation-alert-api by outstanding1301  
  투네이션 / 트윕 Alertbox 후원 알림 수신 기능 구현을 참고했습니다.  
  GitHub: https://github.com/outstanding1301/donation-alert-api

## 프로젝트 목적

투네이션 룰렛 결과를 Unity 타이머와 연동해, 방송 중 룰렛 결과에 따라 시간을 자동으로 추가하거나 차감할 수 있는 구조를 구현하는 것을 목표로 제작했습니다.

일반 타이머 기능과 외부 알림 연동 기능을 분리하여, 연동이 필요 없는 상황에서도 독립적으로 사용할 수 있도록 구성했습니다.
