# Process Guardian Professional (v1.6.0)

<p align="center">
  <img src="./Resources/logo.png" width="200" alt="Process Guardian Logo">
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows-blue?style=for-the-badge&logo=windows" alt="Platform">
  <img src="https://img.shields.io/badge/Language-C%23-green?style=for-the-badge&logo=c-sharp" alt="Language">
  <img src="https://img.shields.io/badge/Version-1.6.0_Professional-orange?style=for-the-badge" alt="Version">
  <img src="https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge" alt="License">
</p>

---

**Process Guardian Professional**은 Windows 환경에서 중요한 프로세스가 종료되는 것을 방지하기 위해 설계된 강력하고 고도화된 프로세스 관리 도구입니다. v1.6.0 업데이트를 통해 Graceful Shutdown, 시작 지연, 스크립트 실행, 웹훅 알림 등 전문가급 기능을 모두 갖추었습니다.

## 📸 Preview

<p align="center">
  <img src="./docs/assets/screenshot_main.png" width="800" alt="Process Guardian Dashboard Preview">
</p>

---

> [!IMPORTANT]
> **전용 런타임**: 이 프로그램은 **.NET 8 Desktop Runtime**이 필요합니다. 실행 시 오류가 발생한다면 [공식 다운로드 링크](https://dotnet.microsoft.com/download/dotnet/8.0)에서 설치해 주세요.

## ✨ Key Features (v1.6.0 New)

### 핵심 기능
- 🛡️ **정밀 경로 감시 (Precise Matching)**: 파일 이름뿐만 아니라 **전체 경로(Full Path)**를 대조하여 서로 다른 폴더의 동일 이름 프로세스를 오판하지 않습니다.
- 🔄 **재시작 백오프 (Restart Backoff)**: 대상 앱이 계속 크래시 날 경우 자동으로 감시 간격을 늘려 시스템 리소스를 보호합니다.
- ⚡ **비동기 모니터링 (Async Engine)**: UI 스레드와 분리된 비동기 루프로 작동하여 다수의 프로세스를 감시해도 UI가 멈추지 않습니다.
- 📜 **실시간 로그 콘솔 (Log Center)**: 하단 로그 창을 통해 복구 성공, 오류, 리소스 경고 등을 타임스탬프와 함께 실시간으로 확인합니다.
- ➕ **동적 슬롯 확장 (Dynamic Slots)**: 고정된 슬롯 방식에서 탈피하여, 버튼 하나로 무제한 감시 대상을 추가할 수 있습니다.
- 💻 **Windows 자동 시작**: 윈도우 부팅 시 가디언이 자동으로 실행되도록 설정할 수 있습니다.
- 🔒 **Graceful Shutdown**: 종료 시 감시 중인 모든 프로세스를 정상적으로 종료한 후 앱을 닫습니다.
- ⏱️ **시작 지연 (Startup Delay)**: 각 프로세스마다 시작 지연 시간을 설정하여 부팅 시 리소스 폭증 방지
- 📜 **시작 전/후 스크립트**: 프로세스 시작 전후에 사용자 정의 스크립트(.bat, .ps1) 자동 실행
- 🔔 **응답 감지 (Hang Detection)**: 프로세스가 응답하지 않을 때 감지하여 알림
- 🪝 **웹훅 알림 (Webhook)**: Slack/Teams/Discord로 실시간 알림
- 📋 **Windows 이벤트 로그**: 시스템 이벤트 로그에 기록
- 📊 **설정 내보내기/가져오기**: JSON으로 설정 백업/복원

### 모니터링
- **메모리 임계값 경고**: 설정된 메모리 초과 시 알림
- **CPU 사용량 표시**: 프로세스별 CPU 사용량 추적
- **실패 횟수 추적**: 재시작 실패 횟수 카운트
- **가동 시간 추적**: 각 프로세스 가동 시간 기록

### 다국어 지원
- 🇰🇷 한국어
- 🇺🇸 English
- 🇯🇵 日本語
- 🇨🇳 简体中文

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 Desktop Runtime
- Windows 10 / 11 (관리자 권한 권장)

### Quick Start
1. **프로그램 실행**: `ProcessGuardian.exe`를 실행합니다.
2. **슬롯 추가**: `+ Add Slot` 버튼을 눌러 새 감시 카드를 생성합니다.
3. **프로그램 선택**: `Browse`를 눌러 감시할 실행 파일을 선택합니다.
4. **설정 조정**: 하단에서 간격, 메모리 임계값, 시작 지연 등을 설정합니다.

### 주요 설정
| 설정 | 설명 | 기본값 |
|------|------|--------|
| Interval | 감시 주기 (초) | 3초 |
| Memory | 메모리 경고 임계값 (MB) | 2048MB |
| Startup Delay | 시작 지연 (초) | 0초 |
| Hang Timeout | 응답 감지 시간 (초) | 30초 |

### 트레이 메뉴
| 메뉴 | 설명 |
|------|------|
| Open Dashboard | 대시보드 열기 |
| Settings > Export | 설정 내보내기 |
| Settings > Import | 설정 가져오기 |
| Exit Guardian | 종료 |

## 📥 다운로드

GitHub Releases에서 최신 버전을 다운로드하세요:
https://github.com/jeiel85/process-guardian/releases

**ProcessGuardian.exe** (~65MB, Self-contained)
- .NET 런타임无需설치
- 바로 실행 가능

## 🛠️ Built With

- **C# 12 / .NET 8** - Modern Logic & Core
- **Windows Forms** - Professional Dark UI
- **GDI+** - Custom Rendered Cards & LED Indicators
- **GitHub Actions** - CI/CD

## 📝 License

Distributed under the MIT License. See `LICENSE` for more information.

---

<p align="center">
 Developed with ❤️ for a more stable Windows environment.
</p>