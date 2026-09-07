# 절차적 탐사 맵 생성 규격

## 완료 기준

절차적 탐사 맵은 다음 조건을 만족해야 한다.

1. 같은 `mapSeed`, 생성기 버전, 설정 서명은 같은 셀과 심볼을 만든다.
2. Start, Boss, End 심볼은 각각 정확히 하나다.
3. 모든 Floor 셀은 4방향 이동으로 서로 연결된다.
4. Boss와 End는 4방향으로 인접하며 End의 유일한 Floor 이웃은 Boss다.
5. Start에서 Boss까지의 최단 경로는 설정한 최소 길이 이상이다.
6. 일반 심볼은 설정 수량을 정확히 충족하고 한 셀에 하나만 놓인다.
7. 모든 심볼은 Floor에 놓이고 Event와 Item은 각각 필수 페이로드를 가진다.
8. 설정한 두께의 외곽은 모두 Wall이다.
9. 유효한 결과가 나오지 않으면 빈 맵을 적용하지 않고 생성 실패로 처리한다.

## 생성 절차

활성 생성기는 `ExploreMapGenerator` 버전 2다.

1. 설정 유효성 검사
2. 타원형 내부 영역과 외곽 벽 구성
3. 인접한 Boss/End 앵커 배치
4. Boss에서 멀어지는 메인 스파인과 선택적 가지 생성
5. 노드와 복도 굴착
6. Start-Boss 연결 보강, End 진입 봉쇄, 고립 Floor 제거
7. 외곽 봉인
8. 앵커와 설정 기반 심볼 배치
9. 전체 제약 검사

한 시도에서 제약을 만족하지 못하면 원본 `mapSeed`, 설정 서명, 시도 번호로 파생한 시드로 다시 시도한다. 재시도 역시 결정적이므로 같은 입력은 항상 같은 성공 결과 또는 같은 실패를 낸다.

## 설정 규칙

`ExploreMapConfig.generationRules`에서 다음 값을 조정한다.

| 필드 | 의미 | 기본값 |
| --- | --- | ---: |
| `min/maxSpineNodes` | 메인 경로 노드 목표 범위 | 5 / 8 |
| `min/maxNodeSpacing` | 메인 노드 간격 범위 | 2 / 4 |
| `minStartBossPathLength` | Start-Boss 최단 경로 하한 | 8 |
| `branchProbability` | 스파인 노드별 가지 생성 확률 | 0.25 |
| `min/maxBranchLength` | 가지 노드 수 범위 | 1 / 3 |
| `borderThickness` | 강제 외곽 벽 두께 | 1 |
| `minSymbolSpacing` | 심볼 간 최소 체비쇼프 거리 | 2 |
| `eliteSpacingBonus` | Elite 심볼의 추가 간격 | 1 |
| `maxAttempts` | 결정적 최대 재시도 횟수 | 24 |

추가 제약은 다음과 같다.

- 맵 한 변은 9~256 셀이다.
- Start, Boss, End를 `symbolConfig`에 적으면 수량은 반드시 1이어야 한다. 생략해도 생성기가 각 1개를 만든다.
- Event 수량이 1 이상이면 양수 가중치의 `eventCandidate`가 필요하다.
- Item 수량이 1 이상이면 양수 가중치의 `itemCandidate`가 필요하다.
- 심볼 설정 순서와 후보 순서는 결정 결과에 포함되므로 단순 재정렬도 설정 서명을 바꾼다.

## 시드 생성·저장·복원

새 탐사에서는 슬롯의 `RngHubStateless`가 `Explore_{Dungeon}_{Floor}` 카테고리로 `mapSeed`를 파생한다. 파생과 동시에 해당 카테고리 카운터가 증가하며, 카운터는 슬롯 JSON의 `RngCounters`에 저장된다. 따라서 같은 층을 다시 시작하면 새 시드를 얻고, 다른 RNG 카테고리의 호출 순서에는 영향을 받지 않는다.

`ExploreSnapshot`에는 다음 재현 계약을 저장한다.

| 값 | 역할 |
| --- | --- |
| `mapSeed` | 해당 맵의 원본 시드 |
| `mapGenerationVersion` | 사용할 생성 알고리즘 버전 |
| `mapConfigSignature` | 크기, 생성 규칙, 심볼 수량과 후보 가중치의 64비트 서명 |

복원 시 저장된 세 값을 사용해 스켈레톤을 재생성한다. 버전 1 저장은 서명이 없으므로 레거시 생성 경로를 사용한다. 버전 2 저장에서 현재 설정 서명이 다르면 다른 맵을 조용히 생성하지 않고 복원을 실패시킨다. 생성 규칙을 출시 후 변경하려면 기존 설정 에셋을 보존하거나 생성기 버전을 올리고 호환 경로를 추가해야 한다.

## 검증 방법

Unity 메뉴에서 `AngelBeat > Validation > Verify Procedural Explore Maps`를 실행한다. 검증기는 모든 `ExploreMapConfig`에 대해 다음을 수행한다.

- 설정당 100개 시드 생성
- 각 시드 즉시 재생성 후 스켈레톤 서명 비교
- 버전 1 대표 시드의 레거시 생성·재생성 비교
- 모든 런타임 제약 검사
- 90% 이상의 고유 레이아웃 확인
- 잘못된 설정 서명으로 복원 시 거부되는지 확인

현재 기본 설정 검증 결과는 두 설정에서 각각 100/100 고유 레이아웃이다. 버전 2 원본과 재생성 400개 및 버전 1 호환 경로 4개를 합쳐 총 404개 스켈레톤을 검사한다.

배치 실행은 다음 진입점을 사용한다.

```text
-batchmode -projectPath <project> \
-executeMethod AngelBeat.Editor.Tools.ExploreMapGenerationVerifier.RunBatchAndExit \
-logFile <log-path>
```

저장 계약은 `SaveTest`의 JSON 왕복 및 깊은 복사 테스트가 `mapSeed`, 생성기 버전, 설정 서명을 함께 보존하는지 검증한다.
