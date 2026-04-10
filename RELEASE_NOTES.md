---

# Release Notes: Process Guardian v1.2.0 (Premium UI & Dual Distribution)

이번 버전은 사용자 인터페이스의 완성도를 상용 수준으로 끌어올리고, 사용자 환경에 맞춘 유연한 배포 방식을 도입한 메이저 업데이트입니다.

## 🎨 주요 업데이트: Premium Aesthetic UI

### 1. 시각적 고도화 (Advanced Visuals)
- **둥근 모서리 카드(Rounded Cards)**: 모든 프로세스 모니터링 슬롯에 둥글고 세련된 디자인을 적용하여 현대적인 대시보드 느낌을 완성했습니다.
- **LED 글로우 효과(Glow Effects)**: 상태 표시등에 실제 빛이 발산되는 듯한 글로우 효과를 프로그래밍하여 시각적 인지도를 대폭 향상했습니다.
- **프리미엄 다크 테마**: 목업 디자인에 기반한 깊이 있는 그래파이트 톤과 세련된 블루 포인트 컬러를 적용했습니다.

### 2. 배포 방식의 이원화 (Dual Distribution)
사용자의 환경에 맞춰 두 가지 방식의 실행 파일을 모두 제공합니다.

- **🚀 Slim Edition**: 
  - 파일 용량이 매우 작음 (약 0.1MB)
  - 실행을 위해 .NET 8.0 Desktop Runtime 설치가 필요합니다.
- **📦 Standalone Edition**: 
  - 별도의 설치 없이 즉시 실행 가능
  - .NET 런타임이 내장되어 있어 용량이 크지만(약 160MB), 가장 안정적인 실행을 보장합니다.

### 3. 기술 및 안정성 개선
- **Single-File 컴파일**: 모든 관련 라이브러리를 하나로 통합하여 단일 파일로 배포합니다.
- **UI 성능 최적화**: GDI+ 기반의 사용자 정의 그리기 로직을 최적화하여 저사양 PC에서도 부드럽게 작동합니다.

---

# Release Notes: Process Guardian v1.1.0 (Optimization & Global Support)

이번 버전은 프로그램 경량화와 글로벌 사용자를 위한 다국어 지원에 집중한 고도화 릴리즈입니다.

## 🚀 주요 업데이트

### 1. 파격적인 용량 절감 (177MB → 0.16MB)
- 배포 방식을 `Framework-dependent`로 전환하여 프로그램 용량을 **1000배** 가깝게 줄였습니다.
- 이제 매우 가벼운 상태로 배포 및 유통이 가능합니다. (※ 실행 시 .NET 8 Desktop Runtime 필요)

### 2. 다국어 지원 (Multilingual Support)
- **한/영/중/일** 4개 국어를 정식 지원합니다.
- 시스템 언어를 자동으로 감지하며, 대시보드 하단 설정 메뉴를 통해 언제든지 언어를 변경할 수 있습니다.

### 3. 사용자 안내 강화
- README에 `.NET 8` 필수 설치 안내 및 다운로드 링크를 추가하여 최초 실행 시의 혼란을 방지했습니다.

---

# Release Notes: Process Guardian v1.0.0 (Official First Release)

이번 정식 릴리즈는 **Process Guardian** 프로젝트의 첫 번째 공식 릴리즈로, 기존의 단순한 샘플 수준을 넘어 상용 프로그램급의 완성도를 갖추었습니다.

## 🚀 주요 하이라이트

### 1. 현대적인 UI/UX 개편 (Modern Dark Dashboard)
- **Modern Dark 테마** 도입: 최신 윈도우 스타일의 다크 모드를 지원하여 시각적 피로도를 줄이고 전문적인 외관을 완성했습니다.
- **카드형 모니터링 시스템**: 각 프로세스 슬롯을 독립적인 카드로 설계하여 한눈에 모든 상태를 파악할 수 있습니다.
- **실시간 LED 지표**: 프로세스의 상태(Running, Stopped, Restarting)를 색상 기반의 LED 아이콘으로 즉각 시각화합니다.

### 2. 기술적 개선
- **.NET 8.0 마이그레이션**: 최신 닷넷 프레임워크로 전환하여 성능과 보안성을 대폭 강화했습니다.
- **Self-contained 배포**: 별도의 .NET 런타임 설치 없이도 실행 가능한 단일 파일(`.exe`) 배포 방식을 지원합니다.
- **한글 주석 및 인코딩 복구**: 소스 코드의 인코딩을 UTF-8로 전면 전환하여 개발 편의성을 높였습니다.

### 3. 안정성 강화
- **트레이 모드 최적화**: 백그라운드 작동 시 시스템 리소스 사용량을 최소화했습니다.
- **자동 복구 로직**: 예기치 않게 종료된 프로세스를 3초 내에 감지하여 안정적으로 재실행합니다.

---

## 📥 다운로드 및 설치
1. `dist/ProcessGuardian.exe` 파일을 다운로드합니다.
2. 원하는 위치에 복사한 후 실행하면 즉시 감시가 시작됩니다.

## 📝 라이선스
본 프로젝트는 MIT 라이선스를 따릅니다.

---

Developed by **Process Guardian Team**. 
For more information, visit the [GitHub Repository](https://github.com/jeiel85/process-guardian).
