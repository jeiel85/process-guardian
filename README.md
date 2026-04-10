# Process Guardian Professional (v1.3.0)

<p align="center">
  <img src="./Resources/logo.png" width="200" alt="Process Guardian Logo">
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows-blue?style=for-the-badge&logo=windows" alt="Platform">
  <img src="https://img.shields.io/badge/Language-C%23-green?style=for-the-badge&logo=c-sharp" alt="Language">
  <img src="https://img.shields.io/badge/Version-1.3.0_Professional-orange?style=for-the-badge" alt="Version">
  <img src="https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge" alt="License">
</p>

---

**Process Guardian Professional**은 Windows 환경에서 중요한 프로세스가 종료되는 것을 방지하기 위해 설계된 강력하고 고도화된 프로세스 관리 도구입니다. v1.3.0 업데이트를 통해 정밀한 감시 로직과 재시작 보호 시스템, 그리고 무제한 슬롯 확장 기능을 갖춘 전문가급 툴로 진화했습니다.

## 📸 Preview

<p align="center">
  <img src="./docs/assets/screenshot_main.png" width="800" alt="Process Guardian Dashboard Preview">
</p>

---

> [!IMPORTANT]
> **전용 런타임**: 이 프로그램은 **.NET 8 Desktop Runtime**이 필요합니다. 실행 시 오류가 발생한다면 [공식 다운로드 링크](https://dotnet.microsoft.com/download/dotnet/8.0)에서 설치해 주세요.

## ✨ Key Features (v1.3.0 New)

- 🛡️ **정밀 경로 감시 (Precise Matching)**: 파일 이름뿐만 아니라 **전체 경로(Full Path)**를 대조하여 서로 다른 폴더의 동일 이름 프로세스를 오판하지 않습니다.
- 🔄 **재시작 백오프 (Restart Backoff)**: 대상 앱이 계속 크래시 날 경우 자동으로 감시 간격을 늘려 시스템 리소스를 보호합니다.
- ⚡ **비동기 모니터링 (Async Engine)**: UI 스레드와 분리된 비동기 루프로 작동하여 다수의 프로세스를 감시해도 UI가 멈추지 않습니다.
- 📜 **실시간 로그 콘솔 (Log Center)**: 하단 로그 창을 통해 복구 성공, 오류, 리소스 경고 등을 시보와 함께 실시간으로 확인합니다.
- ➕ **동적 슬롯 확장 (Dynamic Slots)**: 고정된 슬롯 방식에서 탈피하여, 버튼 하나로 무제한 감시 대상을 추가할 수 있습니다.
- 💻 **Windows 자동 시작**: 윈도우 부팅 시 가디언이 자동으로 실행되도록 설정할 수 있습니다.

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 Desktop Runtime
- Windows 10 / 11 (관리자 권한 권장)

### Manual
1. **프로그램 실행**: `ProcessGuardian.exe`를 실행합니다.
2. **슬롯 추가**: `+ Add Slot` 버튼을 눌러 새 감시 카드를 생성합니다.
3. **프로그램 선택**: `Browse`를 눌러 서비스할 실행 파일을 선택합니다.
4. **감시 간격 설정**: 하단에서 데이터 조회 주기를 조절할 수 있습니다 (기본 3초).

### 📥 배포 옵션 (Download Options)
GitHub Releases에서 환경에 맞는 파일을 선택하세요:

- **Slim 버전 (`ProcessGuardian_v1.3.0_Slim.exe`)**: 매우 작은 용량(180KB), [.NET 8 런타임](https://dotnet.microsoft.com/download/dotnet/8.0) 필수.
- **Standalone 버전 (`ProcessGuardian_v1.3.0_Standalone.exe`)**: 런타임 없이 즉시 실행 가능 (약 75MB).

## 🛠️ Built With

- **C# 12 / .NET 8** - Modern Logic & Core
- **Windows Forms** - Professional Dark UI
- **GDI+** - Custom Rendered Cards & LED Indicators

## 📝 License

Distributed under the MIT License. See `LICENSE` for more information.

---

<p align="center">
  Developed with ❤️ for a more stable Windows environment.
</p>
