---

# Release Notes: Process Guardian v1.4.0 (Professional Edition)

## 🚀 주요 업데이트 / 主要更新 / 主なアップデート / Key Updates

### 1. 모니터링 엔진 고도화 / 监控引擎升级 / 監視エンジンの高度化
- **절대 경로 정밀 매칭**: `MainModule.FileName` 기반 경로 비교로 동일 이름 프로세스 구분
- **绝对路径精确匹配**: 使用 `MainModule.FileName` 进行路径比较，区分同名不同路径的进程
- **絶対パス精度マッチング**: `MainModule.FileName`ベースのパス比較で同名プロセスを区別
- **Precision Path Matching**: Uses `MainModule.FileName` to distinguish processes with same name but different paths
- **비동기 논블로킹**: 백그라운드 태스크로 UI 블로킹 방지
- **异步非阻塞**: 后台任务防止UI阻塞
- **非同期ノンブロッキング**: バックグラウンドタスクでUIブロックを防止
- **Async Non-blocking**: Background tasks prevent UI blocking
- **슬롯 삭제 기능**: 추가된 슬롯을 개별적으로 제거 가능
- **插槽删除功能**: 可单独删除已添加的插槽
- **スロット削除機能**: 追加したスロットを個別に削除可能
- **Slot Deletion**: Remove added slots individually
- **프로세스 시작 인수 지원**: 실행 시 명령줄 인수 전달 가능
- **进程启动参数支持**: 启动时传递命令行参数
- **プロセス開始引数サポート**: 起動時コマンドライン引数を伝達
- **Process Start Args**: Pass command-line arguments on startup
- **메모리 임계값 설정**: 메모리 경고 임계값 UI에서 조정
- **内存阈值设置**: 在UI中调整内存警告阈值
- **メモリ閾値設定**: UIでメモリ警告閾値を調整
- **Memory Threshold**: Adjust memory warning threshold in UI
- **파일 로그 저장**: 영구 이벤트 로그 파일 저장
- **文件日志保存**: 永久保存事件日志文件
- **ファイルログ保存**: 永久保存イベントログファイル
- **File Logging**: Permanent event log file storage

### 2. 크래시 루프 보호 (Restart Backoff) / 重启保护
- 연속 실패 시 자동 백오프 알고리즘 적용
- 连续失败时自动应用退避算法
- 連続失敗時に自動バックオフアルゴリズム適用
- **Automatic Backoff**: Applies backoff algorithm on continuous failure
- 전역 예외 처리 및 크래시 로그
- 全局异常处理和崩溃日志
- グローバル例外処理とクラッシュログ
- **Global Exception Handling**: Catches crashes and logs them

### 3. 동적 슬otáms 시스템 / 动态插槽系统
- 무제한 슬롯 추가 (동적 리스트)
- 无限���加插槽(动态列表)
- **Unlimited Slots**: Dynamic list for unlimited monitoring targets
- FlowLayoutPanel 기반 효율적 스크롤
- 基于FlowLayoutPanel的高效滚动
- **FlowLayoutPanel**: Efficient scrolling layout

### 4. 사용자 편의 / 用户便利 / ユーザー利便
- **실시간 로그 콘솔**: 타임스탬프와 함께 실시간 이벤트 확인
- **实时日志控制台**: 带时间戳的实时事件
- **リアルタイムログコンソール**: タイムスタンプ付きリアルタイムイベント
- **Real-time Log Console**: Real-time events with timestamps
- **Windows 자동 시작**: 레지스트리 HKCU 기반 부팅 자동 실행
- **Windows自动启动**: 基于注册表HKCU的启动自动运行
- **Windows自動開始**: レジストリHKCUベースの起動自動実行
- **Auto Start**: Auto-run on Windows boot via registry
- **다국어 지원**: 영어/한국어/일본어/중국어
- **多语言支持**: 英语/韩语/日语/中文
- **多言語サポート**: 英語/韓国語/日本語/中国語
- **Multilingual**: English, Korean, Japanese, Chinese
- **관리자 모드 표시**: 실시간 권한 상태 UI 알림
- **管理员模式显示**: 实时权限状态UI通知
- **管理者モード表示**: リアルタイム権限状態UI通知
- **Admin Mode Indicator**: Real-time privilege status UI
- **리소스 워치독**: 메모리 임계값 초과 시 경고
- **资源看门狗**: 超过内存阈值时警告
- **リソースウォッチドッグ**: メモリ閾値超過時に警告
- **Resource Watchdog**: Warns when memory threshold exceeded

---

# Release Notes: Process Guardian v1.3.0 (Professional Edition)

이번 버전은 **"Professional"**이라는 이름에 걸맞게 내부 로직의 정밀도를 상용 수준으로 높이고, 사용자 편의를 위한 강력한 부가 기능들을 대거 탑재한 기념비적인 업데이트입니다.

## 🚀 주요 업데이트

### 1. 정밀 모니터링 엔진 (Advanced Monitoring)
- **절대 경로 정밀 매칭**: 프로세스 이름에 의존하던 기존 방식에서 탈피하여, 실행 파일의 **절대 경로(`MainModule.FileName`)**를 대조합니다. 이제 경로가 다른 동일 이름의 프로그램들을 오차 없이 정확하게 구분하여 감시합니다.
- **비동기 논블로킹 로직**: 모든 프로세스 조회 및 복구 작업을 별도의 백그라운드 태스크로 분리하여, 다수의 대상을 감시할 때도 메인 UI의 반응성이 항상 최상으로 유지됩니다.

### 2. 크래시 루프 보호 시스템 (Restart Backoff)
- **지능형 재시도 제어**: 대상 프로그램이 실행 직후 계속 종료되는 문제를 감지합니다. 연속 실패 시 감시 간격을 자동으로 늘리는 **백오프(Backoff)** 알고리즘을 도입하여 시스템 무한 루프와 리소스 낭비를 원천적으로 방지합니다.
- **전역 예외 처리**: 가디언 자체의 안정성을 위해 전역 예외 처리 시스템을 도입하고, 심각한 오류 발생 시 `crash.log`에 내용을 기록하여 사후 대응이 가능하도록 개선했습니다.

### 3. 사용자 편의 기능 및 UI 확장
- **실시간 로그 콘솔 (Event Console)**: 대시보드 하단에 실시간 이벤트 로그 창을 추가했습니다. 프로세스 복구, 오류 알림, 리소스 경고 등을 타임스탬프와 함께 선명하게 확인할 수 있습니다.
- **동적 슬롯 확장 (Unlimited Slots)**: 고정된 5개 슬롯의 한계를 깨고, 사용자가 원하는 만큼 감시 대상을 추가할 수 있는 리스트 기반 UI로 개편되었습니다.
- **Windows 자동 시작**: 윈도우 부팅 시 가디언이 자동으로 실행되도록 설정할 수 있는 옵션을 탑재했습니다.
- **관리자 권한 상태 알림**: 앱 실행 권한 상태를 실시간으로 체크하여 UI 상단에 직관적인 경고 아이콘을 제공합니다.

### 4. 기타 개선 사항
- **리소스 워치독**: 감시 대상 앱의 메모리 사용량이 비정상적으로 높을 경우 로그를 통해 사용자에게 경고를 전달합니다.
- **UI 반응성 향상**: `FlowLayoutPanel` 도입으로 다수의 슬롯 관리 시에도 효율적인 스크롤과 레이아웃을 보장합니다.

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
  - .NET 런타임이 내장되어 있어 용량이 크지만 최적화 옵션을 통해 약 71MB로 제공됩니다.

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
