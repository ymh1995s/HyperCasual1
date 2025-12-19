# Copilot 지시 파일

## 프로젝트 개요
Unity 기반 하이퍼캐주얼 러너 모바일 게임 프로젝트

## 기술 스택
- **엔진**: Unity(2020.3 LTS 이상 호환)
- **C# 버전**: C# 9.0
- **.NET 타겟**: .NET Framework 4.7.1
- **플랫폼**: Mobile (iOS/Android) + Unity Editor 지원
- **프로젝트 타입**: Assembly-CSharp (Unity 표준 구조)

## 코딩 규칙

### 네이밍 컨벤션
- **Private 필드**: 언더스코어 접두사 사용('_fieldName')
- **SerializeField**: Inspector에 노출할 private 필드에 항상 사용('[SerializedField] private int _score_')
- **메서드**: PascalCase 사용('예: CalculateScore()')
- **클래스**: PascalCase 사용('예: PlayerController')
- **변수**: __camelCase 사용('예: _speed')

## 파일별 역할
- Assets/Scripts/Player.cs
  - 역할: 플레이어 컨트롤러(하이퍼캐주얼 러너의 플레이어 이동 처리).
  - 동작 요약: 기본적으로 Forward 방향으로 지속 이동하며, 화면을 터치/클릭 후 좌우로 드래그하면 플레이어가 좌우로 이동한다. 마우스(에디터/PC)와 터치(모바일)를 모두 지원.
  - 주요 인스펙터 필드:
    - _forwardSpeed (float): 전진 속도 (기본값 예: 5f)
    - _lateralSpeed (float): 화면 드래그 대비 좌우 이동 크기 (기본값 예: 10f)
    - _lateralSmooth (float): 목표 X로 보간하는 부드러움 계수 (기본값 예: 10f)
    - _maxX (float): 좌우 이동 최대 범위 (예: 3f)
  - 입력 처리: Input.touchCount, Touch.phase 및 Input.GetMouseButton*를 사용하여 드래그 시작/이동/종료를 관리. 화면 가로 픽셀 이동을 Screen.width로 정규화해 다양한 해상도에서 일관된 감도 제공.
  - 권장 개선 사항: Rigidbody 기반 물리 이동으로 변경하거나 데드존(무시할 작은 드래그 값) 추가, 모바일별 민감도 튜닝 등.