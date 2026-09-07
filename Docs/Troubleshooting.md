# AngelBeat 문제 해결 기록

재현이 어렵거나 비동기 호출 경계 때문에 원인이 흐려질 수 있는 문제를 기록한다. 각 항목에는 증상, 실제 원인, 해결 방법과 회귀 검증을 함께 남긴다.

## 2026-09-02: 적 AI 턴 종료 시 `MissingReferenceException`

### 증상

PlayMode 테스트 `DebugBattle_EnemyAiTurnCompletesAndReturnsControl`에서 적 AI가 행동한 뒤 플레이어에게 제어권이 돌아왔지만 테스트가 실패했다.

처음에는 `OperationCanceledException`이 미처리 로그로 보였고, 취소 전파를 정리한 뒤에는 다음 오류가 드러났다.

```text
MissingReferenceException: The object of type 'CharMonster' has been destroyed
but you are still trying to access it.
Turn.ExecuteMonsterAI(CharMonster monster) (Turn.cs)
```

호출 스택의 하단은 다음 순서였다.

```text
CharJump의 UniTask.Delay 취소
→ JumpBattleAction
→ CharacterAI / CharMonster
→ Turn.ExecuteMonsterAI
→ UniTask.Forget
→ UniTaskScheduler.PublishUnobservedTaskException
```

### 원인

전투 종료 시 `BattleCharManager.ClearAll()`이 전투 캐릭터를 파괴한다. 동시에 진행 중이던 점프 애니메이션의 지연 작업은 정상적으로 취소되어 `OperationCanceledException`이 `Turn.ExecuteMonsterAI()`까지 전파된다.

문제는 취소 자체가 아니라 취소 처리 블록에서 이미 파괴된 Unity 객체의 `monster.name`을 다시 읽은 것이었다. 이 접근이 새로운 `MissingReferenceException`을 만들었다.

`catch (OperationCanceledException)` 내부에서 발생한 새 예외는 같은 `try` 뒤의 `catch (Exception)`이 잡지 않는다. 또한 새 예외는 취소 예외가 아니므로 `SuppressCancellationThrow()`의 대상도 아니다. 마지막 fire-and-forget 경계인 `.Forget()`은 이 관찰되지 않은 예외를 전역 스케줄러에 게시했고, Unity Test Runner가 실패 로그로 처리했다.

즉 `.Forget()`은 오류를 만든 직접 원인이 아니라, 수명 관리되지 않은 예외가 최종적으로 드러난 위치였다.

### 해결

`Turn.ExecuteMonsterAI()` 진입 직후, 첫 `await` 전에 몬스터 이름을 일반 문자열로 저장한다. 이후 정상 완료, 취소, 오류 로그는 저장한 문자열만 사용하고 파괴될 수 있는 `CharMonster`를 다시 역참조하지 않는다.

AI 및 턴 전환의 fire-and-forget 진입점은 다음 규칙을 적용한다.

- `UniTaskVoid` 대신 `UniTask`를 반환한다.
- 정상적인 수명 종료 취소는 `SuppressCancellationThrow()`로 소비한다.
- 일반 예외는 취소와 구분해 로그로 남기고 턴 종료 콜백을 유지한다.
- `await` 이후에는 파괴될 수 있는 `UnityEngine.Object`의 프로퍼티를 읽지 않는다. 필요한 표시 값은 작업 시작 전에 복사한다.

### 함께 수정한 테스트 레이스

`TurnController.ChangeTurn()`은 `CurrentTurn`을 먼저 교체한 뒤 카메라 포커스 등 `OnTurnBeganAsync` 작업을 계속 수행한다. 테스트가 `CurrentTurn == Player`만 확인하고 즉시 `EndBattle()`을 호출하면 전환 중인 작업과 씬 정리가 겹칠 수 있다.

따라서 적 AI 회귀 테스트는 다음 두 조건을 모두 만족한 뒤 전투를 정리한다.

- 현재 턴이 Player다.
- `TurnController._isChangingTurn`이 `false`다.

이 조건은 “플레이어 턴 객체가 선택됨”이 아니라 “플레이어 턴 전환 작업이 완료됨”을 검증한다.

### 검증 결과

Unity Test Runner에서 `BattleFlowPlayModeTest`의 다음 네 시나리오가 모두 통과했다.

1. 실제 로딩 경로로 전투 진입 후 첫 턴 도달
2. 적 AI 행동 완료 후 플레이어 제어권 복귀
3. 종료 중 `GameManager` 재생성 방지
4. 다중 스테이지 승리 및 패배 후 재시작 루프

### 재발 시 확인 순서

1. 스택 최하단에서 최초 취소 또는 파괴 지점을 찾는다.
2. `Forget`이나 전역 스케줄러보다 먼저, 가장 가까운 사용자 코드의 catch/finally를 확인한다.
3. catch/finally가 파괴된 `UnityEngine.Object`를 로그나 상태 확인 목적으로 다시 읽는지 확인한다.
4. 테스트가 상태 플래그만 보고 비동기 전환 완료 전에 정리를 시작하는지 확인한다.
5. 작업을 실제로 중단할 필요가 있다면 객체 수명에 연결된 `CancellationToken`을 전달하고 소유자가 취소한다.

## 2026-09-04: StageEditor Cover 미리보기 좌표 발산

### 증상

`Obstacle / Cover` 페인팅 중 Cover의 위치가 클릭한 셀 근처가 아니라 수천 단위 좌표로 이동하고, 계속 움직이면 `10^13` 이상의 값으로 발산했다.

### 원인과 해결

배치 높이를 계산할 때 이동 중인 미리보기 오브젝트의 `Collider2D.bounds`를 매 Scene GUI 이벤트마다 읽었다. Edit Mode에서는 Transform 변경과 2D 물리 Bounds 캐시 갱신 시점이 일치하지 않을 수 있으므로, 이전 Bounds와 새 Transform의 차이가 Collider 오프셋으로 누적됐다.

물리 Bounds 캐시 사용을 제거하고 `BoxCollider2D.size`, `offset`, 자식 Transform 행렬만으로 루트 기준 네 꼭짓점과 하단 오프셋을 계산한다. 이 계산은 미리보기의 현재 월드 좌표에 의존하지 않으므로 Transform을 반복 갱신해도 위치가 누적되지 않는다.

회귀 확인은 Cover를 여러 셀 사이로 계속 이동한 뒤 배치하여 다음을 확인한다.

- 좌표가 유한한 셀 주변 값으로 유지된다.
- Collider 하단이 선택한 셀 하단 경계와 일치한다.
- 저장 후 다시 열어도 같은 셀에 등록된다.

## 2026-09-04: StageEditor 삭제 후 유령 점유 셀

### 증상

하이어라키나 Stage Summary에서 플랫폼, 장애물 또는 커버를 삭제했는데도 빈 셀이 빨간색으로 표시되며 다시 배치할 수 없었다. 플랫폼을 전부 삭제한 뒤에도 일부 셀이 이미 플랫폼으로 점유된 것으로 판정될 수 있었다.

### 원인

배치 오브젝트와 `StageField`의 직렬화 컬렉션은 별도 상태다.

- `platformGridCells`는 오브젝트 참조 없이 셀 좌표만 보관한다.
- `obstacleGridCells`, `coverageGridCells`는 셀과 컴포넌트 참조를 보관한다.

삭제 직후 하이어라키 변경 콜백이 실행되지 않거나 실행 순서가 늦으면 오브젝트는 사라졌지만 직렬화 컬렉션에는 좌표가 남는다. 배치 가능 판정이 이 컬렉션만 읽으면 남은 좌표를 실제 점유로 오인한다.

`(3, 1)`이 Stored와 Hierarchy 양쪽에 `OK`로 다시 나타난 직접 원인은 빈 `Platforms` 관리 루트였다. 하이어라키 스캔이 이 루트 자체까지 배치 후보로 열거했고, Unity의 `Transform.IsChildOf`가 자기 자신에도 `true`를 반환하기 때문에 `Platforms` 루트가 실제 플랫폼으로 분류됐다. Collider가 없는 루트는 플랫폼 폭 계산의 기본값인 1칸을 사용했고, 루트 위치 `(0, 0)`이 해당 7×2 맵의 셀 `(3, 1)`로 변환됐다. 따라서 컬렉션을 비워도 다음 동기화에서 같은 좌표가 즉시 재등록됐다.

페인팅용 `PreviewObject`도 `HideAndDontSave`라 일반 하이어라키에는 보이지 않으면서 하위 Transform 스캔에는 포함될 수 있으므로, 별도의 방어 대상으로 함께 제외한다. 다만 이번 `(3, 1)` 재등록의 직접 원인은 Preview가 아니라 `Platforms` 루트 자체였다.

### 해결

현재 하이어라키에서 플랫폼, 장애물, 커버 셀을 계산하는 `GridDataSnapshot`을 단일 기준으로 사용한다.

- 배치 가능 판정은 직렬화된 캐시가 아니라 현재 하이어라키 스냅샷을 읽는다.
- `Platforms`, `Objects`, 구형 `ObjectRoot`는 관리용 부모이므로 배치 후보 열거에서 명시적으로 제외한다.
- `InferPlacedKind`에서도 `Platforms` 루트 자기 자신을 거부하여 `IsChildOf`의 자기 자신 판정을 이중 방어한다.
- 읽기 전용 스냅샷 생성 시 `PlatformsRoot` getter를 호출하지 않아, 진단이나 동기화만으로 빈 관리 루트를 새로 만들지 않는다.
- Preview에는 `StageEditorPreviewMarker`를 붙이고, 마커·`PreviewObject` 이름·`HideAndDontSave` 조상 중 하나라도 확인되면 루트와 모든 자식을 스캔에서 제외한다.
- 하이어라키 변경, Undo/Redo, 맵 로드와 Scene GUI Layout에서 저장 컬렉션과 스냅샷을 비교하고 다를 때만 다시 적용한다.
- 파괴된 장애물/커버 참조는 점유 데이터로 취급하지 않는다.
- `Internal Grid Collections`에서 Stored와 Hierarchy 결과를 비교한다. 불일치 셀은 주황색으로 표시한다.
- `Rebuild From Hierarchy`, `Remove Stale Only`, 셀별/Raw 항목별 제거로 잘못된 데이터를 직접 복구할 수 있다.

### 확인 순서

1. Object Mode의 `Internal Grid Collections`에서 Stored/Hierarchy 개수를 비교한다.
2. 주황색 셀을 선택하여 P/O/C 값이 어느 쪽에만 남아 있는지 확인한다.
3. 삭제된 오브젝트의 데이터만 남았다면 `Remove Stale Only`를 실행한다.
4. 전체 상태가 의심스럽다면 `Rebuild From Hierarchy`로 현재 배치물을 기준으로 다시 생성한다.
5. 플랫폼 또는 장애물을 삭제하고 해당 셀이 즉시 회색 또는 정상 배치 가능 색으로 돌아오는지 확인한다.
