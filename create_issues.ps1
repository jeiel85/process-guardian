$repo = "jeiel85/process-guardian"

$issues = @(
    @{
        title = "[Bug] 동적 슬롯 저장 한계: 5개 초과 슬롯은 재시작 시 유실됨"
        label = "bug"
        body  = "## 문제 설명`n`nSaveSettings() 메서드가 Properties.Settings의 Path1~Path5 총 5개 키에만 저장하도록 하드코딩되어 있습니다.`nv1.3.0에서 '무제한 슬롯' 기능이 추가되었으나, 6번째 이후 슬롯에 등록된 실행파일 정보는 앱 재시작 시 복구되지 않습니다.`n`n## 재현 방법`n1. 슬롯 6개 이상 추가 후 각각 실행파일 지정`n2. 앱 종료 후 재시작`n3. 6번째 이후 슬롯이 모두 사라짐`n`n## 해결 방안`nJSON 직렬화 방식으로 설정 저장 전환 (AppData\ProcessGuardian\settings.json)"
    },
    @{
        title = "[Bug] 슬롯 삭제 기능 없음 - 추가 후 개별 제거 불가"
        label = "bug"
        body  = "## 문제 설명`n`n슬롯을 한 번 추가하면 개별 삭제 버튼이 없어 제거할 수 없습니다. 불필요한 슬롯이 계속 남아 관리가 어렵습니다.`n`n## 기대 동작`n`n각 슬롯 카드에 X(삭제) 버튼을 추가하여 개별 슬롯을 제거할 수 있어야 함`n`n## 우선순위`n`nHigh - 기본적인 CRUD 중 Delete가 없는 상태"
    },
    @{
        title = "[Bug] 언어 설정이 저장되지 않아 재시작 시 항상 English로 초기화됨"
        label = "bug"
        body  = "## 문제 설명`n`n하단의 언어 드롭다운에서 한국어 등으로 변경해도, 앱 재시작 시 항상 영어(English, index 0)로 초기화됩니다.`n`n## 재현 방법`n1. 언어를 '한국어'로 변경`n2. 앱 종료 후 재실행`n3. 언어가 'English'로 초기화됨`n`n## 해결 방안`n선택된 언어 인덱스를 설정 파일에 저장하고 시작 시 복원"
    },
    @{
        title = "[Feature] 슬롯별 커스텀 이름 지정 기능"
        label = "enhancement"
        body  = "## 기능 설명`n`n현재 슬롯은 PROCESS SLOT 1 같은 자동 이름만 표시됩니다.`n사용자가 슬롯에 의미 있는 이름(예: 게임 클라이언트, 업무용 앱)을 직접 지정할 수 있어야 합니다.`n`n## 제안`n- 슬롯 라벨을 더블클릭하면 인라인 편집 가능`n- 또는 카드 상단에 작은 이름 입력 TextBox 추가"
    },
    @{
        title = "[Feature] 실시간 복구 통계 및 업타임 표시"
        label = "enhancement"
        body  = "## 기능 설명`n`n각 슬롯별로 아래 통계를 표시:`n- 총 복구(재시작) 횟수`n- 마지막 복구 시각`n- 감시 시작 후 경과 시간(업타임)`n`n## 제안 UI`n슬롯 카드 하단에 소형 통계 바 또는 툴팁으로 표시"
    },
    @{
        title = "[Feature] 로그 영구 저장 및 내보내기 기능"
        label = "enhancement"
        body  = "## 기능 설명`n`n현재 하단 로그 콘솔은 앱 실행 중에만 표시되고 종료 시 소실됩니다.`n`n## 제안`n- 로그를 AppData\ProcessGuardian\logs\YYYY-MM-DD.log 파일로 자동 저장`n- 로그 창에 '내보내기' 버튼 추가 (파일 저장 or 클립보드 복사)`n- 오래된 로그 자동 정리 정책 (예: 30일 이상 삭제)"
    },
    @{
        title = "[Feature] 슬롯별 감시 일시정지 / 재개 토글 기능"
        label = "enhancement"
        body  = "## 기능 설명`n`n특정 슬롯만 일시적으로 감시를 멈추고 싶은 경우(예: 점검 중인 앱)가 있습니다.`n현재는 슬롯 경로를 비우거나 전체 감시를 멈추는 방법밖에 없습니다.`n`n## 제안`n- 슬롯 카드에 Pause/Resume 토글 버튼 추가`n- 일시정지 상태: LED 회색, 상태텍스트 PAUSED"
    },
    @{
        title = "[Feature] 프로세스 시작 인수(Command-line Arguments) 설정 지원"
        label = "enhancement"
        body  = "## 기능 설명`n`n일부 프로그램은 특정 인수(예: --minimized, --port 8080)와 함께 실행되어야 합니다.`n현재는 Process.Start(slot.Path)만 호출하여 인수를 전달할 수 없습니다.`n`n## 제안`n- 슬롯 카드에 Arguments 입력 텍스트박스 추가 (접기/펼치기 가능)`n- ProcessStartInfo를 활용하여 인수 포함 실행"
    },
    @{
        title = "[Feature] 설정 내보내기 / 가져오기 (JSON 백업 및 복원)"
        label = "enhancement"
        body  = "## 기능 설명`n`nPC 교체나 재설치 시 모든 슬롯 설정을 다시 입력해야 합니다.`n설정을 JSON으로 내보내고 가져올 수 있으면 편리합니다.`n`n## 제안`n- 상단 메뉴 또는 트레이 우클릭에 '설정 내보내기 (.json)', '설정 가져오기' 추가"
    },
    @{
        title = "[Enhancement] 관리자 권한으로 재실행 버튼 추가"
        label = "enhancement"
        body  = "## 문제 설명`n`n현재 일반 사용자 권한으로 실행 시 '⚠ USER MODE' 경고만 표시되며, 관리자 권한으로 상승하려면 수동으로 앱을 재실행해야 합니다.`n`n## 제안`n- USER MODE 경고 라벨 옆에 '관리자로 재실행' 버튼 추가`n- 클릭 시 ProcessStartInfo { UseShellExecute = true, Verb = 'runas' }로 현재 앱 재시작"
    },
    @{
        title = "[Enhancement] 알림 정책 세분화 (소리/팝업 개별 on-off 제어)"
        label = "enhancement"
        body  = "## 기능 설명`n`n현재 트레이 BalloonTip 팝업은 항상 표시되며 제어할 수 없습니다.`n방해금지 모드나 조용히 작업할 때 불편합니다.`n`n## 제안`n설정 영역에 알림 옵션 체크박스 추가:`n- [x] 트레이 팝업 알림`n- [ ] 소리 알림 (시스템 사운드)`n- [ ] 방해금지 모드 (특정 시간대 알림 끄기)"
    }
)

foreach ($issue in $issues) {
    Write-Host "등록 중: $($issue.title)"
    $params = @("issue", "create", "--repo", $repo, "--title", $($issue.title), "--label", $($issue.label), "--body", $($issue.body))
    & gh $params
    Write-Host "완료!"
    Start-Sleep -Seconds 1
}

Write-Host "`n모든 이슈 등록 완료!"
