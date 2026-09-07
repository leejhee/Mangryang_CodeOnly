# 망량기담 — 게임 클라이언트 코드

2025년 졸업 프로젝트 **망량기담**에서 개발·수정한 게임 클라이언트 코드와 후속 보완 내용을 정리한 코드 열람용 저장소입니다. 포트폴리오에서 다룬 탐사 맵 생성·StageEditor뿐 아니라 게임 진행, 전투, 캐릭터, UI, 저장 및 공통 시스템을 포함합니다.

NovelEngine 본체는 포함하지 않았으며, 연결되는 게임 측 코드는 존재합니다..

## 코드 탐색

| 영역 | 위치 |
| --- | --- |
| 게임 관리와 데이터·리소스·저장 관리자 | [Assets/Core/Scripts/Managers](Assets/Core/Scripts/Managers) |
| 저장 데이터와 탐사 상태 복원 | [Assets/Core/Scripts/GameSave](Assets/Core/Scripts/GameSave) |
| 씬 전환과 공통 게임 기능 | [Assets/GamePlay/Common/Scripts](Assets/GamePlay/Common/Scripts) |
| 로비 | [Assets/GamePlay/Features/Lobby](Assets/GamePlay/Features/Lobby) |
| 마을 | [Assets/GamePlay/Features/Village](Assets/GamePlay/Features/Village) |
| 탐사 진행과 절차적 맵 생성 | [Assets/GamePlay/Features/Explore](Assets/GamePlay/Features/Explore) |
| 전투 진행·유닛·행동·전투 UI | [Assets/GamePlay/Features/Battle](Assets/GamePlay/Features/Battle) |
| UI 공통 실행 구조 | [Assets/UIs/Runtime](Assets/UIs/Runtime) |
| 행동 트리·경로 탐색·상태 등 모듈 | [Assets/Modules](Assets/Modules) |
| 제작·검증 도구 | [Assets/Editor](Assets/Editor) |
| 테스트 | [Assets/Tests](Assets/Tests) |
| 데이터 생성 프로그램 | [DataGenerator](DataGenerator) |

## 포트폴리오 상세 코드

- 탐사 진행: [ExploreManager.cs](Assets/GamePlay/Features/Explore/Scripts/ExploreManager.cs)
- 맵 생성 진입점: [ExploreMapGenerator.cs](Assets/GamePlay/Features/Explore/Scripts/Map/Logic/ExploreMapGenerator.cs)
- 노드·경로 생성 및 보강: [NodeMapBuilder.cs](Assets/GamePlay/Features/Explore/Scripts/Map/Logic/NodeMapBuilder.cs)
- 생성 결과 표시: [ExploreMap.cs](Assets/GamePlay/Features/Explore/Scripts/Map/Logic/ExploreMap.cs)
- 시드별 생성 결과 확인: [ExploreMapTestRunner.cs](Assets/GamePlay/Features/Explore/Scripts/Map/Test/ExploreMapTestRunner.cs)
- 탐사 저장 상태: [ExploreSnapshot.cs](Assets/Core/Scripts/GameSave/ExploreSnapshot.cs)
- 전투 스테이지 편집 도구: [LevelEditor](Assets/Editor/Tools/LevelEditor)

## 설계 문서

- [데이터와 저장 흐름](Docs/DataAndSaveFlow.md)
- [절차적 탐사 맵](Docs/ProceduralExploreMap.md)
- [적 AI](Docs/EnemyAI.md)
- [문제 재현과 수정 기록](Docs/Troubleshooting.md)

## 포함 범위와 실행 제한

- 원본의 폴더 구조를 유지한 C#·어셈블리 정의·셰이더 소스와 해당 메타데이터를 포함합니다.
- 이미지·음원·폰트·모델·씬·프리팹·애니메이션·게임 데이터 원본, 외부 패키지 본체, 빌드 결과는 제외했습니다.
- NovelEngine 본체를 제외했으므로 관련 참조가 남아 있습니다. 이를 포함해 외부 의존성이 제거된 독립 실행 예제는 아닙니다.
- **이 저장소만으로 Unity 프로젝트를 바로 실행하거나 전체 테스트를 수행할 수는 없습니다.** 구현 검토를 위한 소스 공개본입니다.
- 원본 Unity 버전은 [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt), 패키지 의존성은 [manifest.json](Packages/manifest.json)에 기록되어 있습니다. 의존성 목록은 외부 라이브러리 본체를 재배포하지 않습니다.
- DataGenerator는 별도 .NET Framework 프로젝트입니다. 프로젝트·NuGet 설정과 소스는 포함했지만 설치 패키지, 실행 파일 및 XLSX 원본 데이터는 포함하지 않았습니다.

## 버전 기준

2026년 9월 7일 원본의 **현재 로컬 작업 상태**를 기준으로 복사했습니다. 2025년 개발분 외에 2026년 후속 보완과 아직 커밋하지 않은 변경도 포함하므로, 출시·졸업 당시 버전과 동일한 스냅샷은 아닙니다.

원본과 복사본의 파일 해시는 [SOURCE_MANIFEST.json](SOURCE_MANIFEST.json)에 기록했습니다. 원본 커밋 이력을 이 저장소의 새 이력으로 재현하지 않았습니다.

코드 공개 범위와 팀원 기여 구분은 최종 제출 전 검토 대상입니다. 포함된 팀 코드와 외부 의존성의 이용 조건을 일괄 변경하는 별도 라이선스는 부여하지 않습니다.
