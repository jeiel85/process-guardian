---

# Release Notes: Process Guardian v1.6.0 (Professional Edition)

## 주요 업데이트 (Key Updates)

### 1. Graceful Shutdown
- 종료 시 감시 중인 모든 프로세스를 정상적으로 종료한 후 앱 종료
- CloseMainWindow로 정상 종료 시도, 5초 후 강제 종료

### 2. 시작 지연 (Startup Delay)
- 각 프로세스별 시작 지연 시간 설정 (0-300초)
- 부팅 시 동시에 banyak 프로세스 시작による負荷急増 방지

### 3. 시작 전/후 스크립트
- 프로세스 시작 전 사용자 정의 스크립트 실행 (.bat, .ps1)
- 프로세스 시작 후 스크립트 실행

### 4. Hang Timeout UI
- 응답 감지 시간 UI에서 직접 설정 (5-300초)

### 5. 안정성 개선
- 다양한 버그 수정 및 안정성 향상

---

## 주요 기능 (Features)

| 기능 | 설명 |
|------|------|
| 정밀 경로 감시 | MainModule.FileName 기반 경로 비교 |
| 재시작 백오프 | 연속 실패 시 감시 간격 자동 증가 |
| 비동기 모니터링 | UI 스레드와 분리된 모니터링 |
| 동적 슬롯 | 무제한 감시 대상 추가 |
| Windows 자동 시작 | 레지스트리 기반 부팅 시 실행 |
| 실시간 로그 | 타임스탬프와 함께 실시간 확인 |
| 다국어 지원 | 한국어, 영어, 일본어, 중국어 |
| 웹훅 알림 | Slack/Teams/Discord 연동 |
| Windows 이벤트 로그 | 시스템 로그에 기록 |
| 설정 내보내기/가져오기 | JSON으로 백업/복원 |

---

## 다운로드

**ProcessGuardian.exe** (~65MB, Self-contained)
- .NET 런타임 无需설치
- 바로 실행 가능

[Downloads](https://github.com/jeiel85/process-guardian/releases)

---

## 변경 이력

### v1.6.0 (2026-04-17)
- Graceful Shutdown 추가
- 시작 지연 추가
- 시작 전/후 스크립트 실행
- Hang Timeout UI 추가

### v1.5.0 (2026-04-16)
- 응답성 감지 (Hang Detection)
- 웹훅 알림
- Windows 이벤트 로그
- 설정 내보내기/가져오기
- GitHub Actions CI/CD
- 단위 테스트 프로젝트
- ARM64 빌드 지원

### v1.4.0 (2026-04-15)
- 동적 슬롯 무제한 저장
- 슬롯 삭제 기능
- CPU/메모리 모니터링
- 리소스 임계값 UI
- 프로세스 시작 인수
- 파일 로그 저장

### v1.3.0 (2026-04-10)
- 정밀 경로 감지
- 재시작 백오프
- 사용자 편의/UI 확장

---

Developed by **Process Guardian Team**