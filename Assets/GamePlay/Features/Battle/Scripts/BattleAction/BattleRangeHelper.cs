using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using GamePlay.Common.Scripts.Entities.Character.Components;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Unit;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.BattleAction
{
    public static class BattleRangeHelper
    {
        public static readonly List<Vector2Int> jumpableRange = new()
        {
            //   * * * 
            // *   x   * 
            //   * * *
            new Vector2Int(-1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(1, -1),
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };

        public static BattleActionPreviewData ComputeMoveRangeFromClient(BattleStageGrid grid, CharBase client)
        {
            CharStat stat = client.RuntimeStat;
            long movePoint = stat.GetStat(SystemEnum.eStats.NACTION_POINT);
            
            Vector2Int pivot = grid.WorldToCell(client.CharTransform.position);
            
            return ComputeMoveRangeFromPos(grid, pivot, movePoint);
        }

        public static BattleActionPreviewData ComputeMoveRangeFromPos(
            BattleStageGrid grid, Vector2Int pivot, long movePoint)
        {
            List<Vector2Int> movablePoints = new();
            List<Vector2Int> blockedPoints = new();
            List<Vector2Int> maskedPoints = new();
            #region Right Inspection

            bool blocked = false;
            for (int offset = 1; offset <= (int)movePoint; offset++)
            {
                Vector2Int candidate = new(pivot.x + offset, pivot.y);
                if (grid.IsMaskable(candidate))
                {
                    maskedPoints.Add(candidate);
                    break;
                }
                if (grid.IsWalkable(candidate) && !blocked)
                {
                    movablePoints.Add(candidate);
                }
                else if(blocked)
                {
                    blockedPoints.Add(candidate);
                }
                else
                {
                    blocked = true;
                    blockedPoints.Add(candidate);
                }
            }
            #endregion
            #region Left Inspection

            blocked = false;
            for (int offset = 1; offset <= (int)movePoint; offset++)
            {
                Vector2Int candidate = new(pivot.x - offset, pivot.y);
                if (grid.IsMaskable(candidate))
                {
                    maskedPoints.Add(candidate);
                    break;
                }
                if (grid.IsWalkable(candidate) && !blocked)
                {
                    movablePoints.Add(candidate);
                }
                else if(blocked)
                {
                    blockedPoints.Add(candidate);
                }
                else
                {
                    blocked = true;
                    blockedPoints.Add(candidate);
                }
            }
            #endregion
            
            return new BattleActionPreviewData(movablePoints, blockedPoints, maskedPoints);
        }

        public static BattleActionPreviewData ComputeJumpRangeFromClient(BattleStageGrid grid, CharBase client)
        {
            Vector2Int pivot = grid.WorldToCell(client.CharTransform.position);

            return ComputeJumpRangeFromPos(grid, pivot);   
        }

        public static BattleActionPreviewData ComputeJumpRangeFromPos(BattleStageGrid grid, Vector2Int pivot)
        {
            List<Vector2Int> jumpablePoints = new();
            List<Vector2Int> blockedPoints = new();
            List<Vector2Int> maskedPoints = new();
            foreach (Vector2Int candidate in jumpableRange)
            {
                Vector2Int sum = pivot + candidate;
                maskedPoints.Add(sum);
                if (grid.IsMaskable(sum)) continue;
                if (grid.IsWalkable(sum))
                {
                    jumpablePoints.Add(sum);
                }
                else
                {
                    blockedPoints.Add(sum);
                }
            }
            var masked = maskedPoints.Except(jumpablePoints).ToList();
            masked = masked.Except(blockedPoints).ToList();
            return new BattleActionPreviewData(jumpablePoints, blockedPoints, masked);
        }
        
        private static bool IsMaskMatch(BattleStageGrid grid, Vector2Int coord, SystemEnum.eCharType maskType)
        {
            if (!grid.IsOccupied(coord) || maskType == SystemEnum.eCharType.None) return false;
            CharBase candidate = grid.GetUnitAt(coord);
            if (!candidate) return false;
            return candidate.GetCharType() == maskType;
        }
        
        public static BattleActionPreviewData ComputeSkillRange(
            BattleStageGrid grid, 
            SkillRangeData rangeData,
            CharBase client)
        {
            return ComputeSkillRange(
                grid,
                rangeData,
                client.GetCharType(),
                grid.WorldToCell(client.CharTransform.position),
                client.LastDirectionRight);
        }

        /// <summary>
        /// 실제 Transform을 옮기지 않고 가상의 위치와 방향에서 스킬 범위를 계산한다.
        /// AI가 이동 후 공격 가능성을 평가할 때 사용한다.
        /// </summary>
        public static BattleActionPreviewData ComputeSkillRange(
            BattleStageGrid grid,
            SkillRangeData rangeData,
            SystemEnum.eCharType clientType,
            Vector2Int origin,
            bool faceRight)
        {
            List<Vector2Int> targetable = new(); // 지정 가능
            List<Vector2Int> unable = new();    // 지정 불가
            List<Vector2Int> masked = new();

            // Inspect Mask
            SystemEnum.ePivot skillPivot = rangeData.skillPivot;
            
            SystemEnum.eCharType maskType;
            if (skillPivot == SystemEnum.ePivot.TARGET_ENEMY)
                maskType = BattleCharManager.GetEnemyType(clientType);
            else if (skillPivot == SystemEnum.ePivot.TARGET_ALLY)
                maskType = clientType;
            else
                maskType = SystemEnum.eCharType.None;
            
            // Special case
            if (skillPivot == SystemEnum.ePivot.TARGET_SELF)
            {
                targetable.Add(origin); // 만약 나 자신이라면 그냥 나만 넣고 끝내면 됨.
                return new BattleActionPreviewData(targetable, unable, new());
            }
            
            // Calculation
            
            // '앞' 방향 구별
            Vector2Int forward = faceRight ? Vector2Int.right : Vector2Int.left;
            Vector2Int backward = -forward;
            
            // 범위 내 임의선택 가능한지
            bool canAccessAny = rangeData.skillType != SystemEnum.eSkillType.PhysicalAttack;
            
            // 임의선택 불가 시 block해줄 flag
            bool blocked = false;
            
            #region y = 0
            
            if (rangeData.Origin)
            {
                masked.Add(origin);
                if(IsMaskMatch(grid, origin, maskType)) targetable.Add(origin);
                else unable.Add(origin);
            }
            
            for (int i = 1; i <= rangeData.Forward; i++)
            {
                Vector2Int pos = origin + forward * i;
                masked.Add(pos);
                if (!grid.IsInBounds(pos) || !grid.IsPlatform(pos)) continue; // 범위 밖이거나 플랫폼이 아니면 무시
                if (!canAccessAny && blocked)
                {
                    // 물리공격이고 현재 막혀있는 상태면 불가 타일
                    unable.Add(pos);
                    continue;
                }
                
                // 범위 안, 플랫폼.
                if (IsMaskMatch(grid, pos, maskType))
                {
                    blocked = true;
                    targetable.Add(pos);
                }
                else // 캐릭터에 의해 차지되지 않았을 때
                {
                    
                    // 장애물일 때
                    if (grid.IsObstacle(pos))
                    {
                        blocked = true;
                    }

                    unable.Add(pos);
                }
            }
            
            blocked = false;
            for (int i = 1; i <= rangeData.Backward; i++)
            {
                Vector2Int pos =  origin - forward * i;
                masked.Add(pos);
                
                if (!grid.IsInBounds(pos) || !grid.IsPlatform(pos)) continue;
                if (!canAccessAny && blocked)
                {
                    // 물리공격이고 현재 막혀있는 상태면 불가 타일
                    unable.Add(pos);
                    continue;
                }
                
                // 범위 안, 플랫폼.
                if (IsMaskMatch(grid, pos, maskType))
                {
                    blocked = true;
                    targetable.Add(pos);
                }
                else // 캐릭터에 의해 차지되지 않았을 때
                {
                    
                    // 장애물일 때
                    if (grid.IsObstacle(pos))
                    {
                        blocked = true;
                    }

                    unable.Add(pos);
                }
            }
            
            
            #endregion
            
            #region y = 1
            
            if (rangeData.Up)
            {
                Vector2Int pos = origin + Vector2Int.up;
                masked.Add(pos);
                if (grid.IsInBounds(pos) && grid.IsPlatform(pos))
                {
                    if(IsMaskMatch(grid, pos, maskType)) targetable.Add(pos);
                    else unable.Add(pos);
                }
            }
            
            blocked = false;
            for (int i = 1; i <= rangeData.UpForward; i++)
            {
                Vector2Int pos = origin + forward * i + Vector2Int.up;
                masked.Add(pos);
                if (!grid.IsInBounds(pos) || !grid.IsPlatform(pos)) continue;
                
                if (!canAccessAny && blocked)
                {
                    // 물리공격이고 현재 막혀있는 상태면 불가 타일
                    unable.Add(pos);
                    continue;
                }
                
                // 범위 안, 플랫폼.
                if (IsMaskMatch(grid, pos, maskType))
                {
                    blocked = true;
                    targetable.Add(pos);
                }
                else // 캐릭터에 의해 차지되지 않았을 때
                {
                    
                    // 장애물일 때
                    if (grid.IsObstacle(pos))
                    {
                        blocked = true;
                    }

                    unable.Add(pos);
                }
            }
            
            blocked = false;
            for (int i = 1; i <= rangeData.UpBackward; i++)
            {
                Vector2Int pos = origin - forward * i + Vector2Int.up;
                masked.Add(pos);
                if (!grid.IsInBounds(pos) || !grid.IsPlatform(pos)) continue;
                
                if (!canAccessAny && blocked)
                {
                    // 물리공격이고 현재 막혀있는 상태면 불가 타일
                    unable.Add(pos);
                    continue;
                }
                
                // 범위 안, 플랫폼.
                if (IsMaskMatch(grid, pos, maskType))
                {
                    blocked = true;
                    targetable.Add(pos);
                }
                else // 캐릭터에 의해 차지되지 않았을 때
                {
                    
                    // 장애물일 때
                    if (grid.IsObstacle(pos))
                    {
                        blocked = true;
                    }

                    unable.Add(pos);
                }
            }
            #endregion
            
            #region y = -1
            
            
            
            if (rangeData.Down)
            {
                Vector2Int pos = origin + Vector2Int.down;
                masked.Add(pos);
                if (grid.IsInBounds(pos) && grid.IsPlatform(pos))
                {
                    if(IsMaskMatch(grid, pos, maskType)) targetable.Add(origin);
                    else unable.Add(pos);
                }
            }
            
            blocked = false;
            for (int i = 1; i <= rangeData.DownForward; i++)
            {
                Vector2Int pos = origin + forward * i + Vector2Int.down;
                masked.Add(pos);
                if (!grid.IsInBounds(pos) || !grid.IsPlatform(pos)) continue;
                
                if (!canAccessAny && blocked)
                {
                    // 물리공격이고 현재 막혀있는 상태면 불가 타일
                    unable.Add(pos);
                    continue;
                }
                
                // 범위 안, 플랫폼.
                if (IsMaskMatch(grid, pos, maskType))
                {
                    blocked = true;
                    targetable.Add(pos);
                }
                else // 캐릭터에 의해 차지되지 않았을 때
                {
                    
                    // 장애물일 때
                    if (grid.IsObstacle(pos))
                    {
                        blocked = true;
                    }

                    unable.Add(pos);
                }
            }
            
            blocked = false;
            for (int i = 1; i <= rangeData.DownBackward; i++)
            {
                Vector2Int pos = origin - forward * i + Vector2Int.down;
                masked.Add(pos);
                if (!grid.IsInBounds(pos) || !grid.IsPlatform(pos)) continue;
                
                if (!canAccessAny && blocked)
                {
                    // 물리공격이고 현재 막혀있는 상태면 불가 타일
                    unable.Add(pos);
                    continue;
                }
                
                // 범위 안, 플랫폼.
                if (IsMaskMatch(grid, pos, maskType))
                {
                    blocked = true;
                    targetable.Add(pos);
                }
                else // 캐릭터에 의해 차지되지 않았을 때
                {
                    
                    // 장애물일 때
                    if (grid.IsObstacle(pos))
                    {
                        blocked = true;
                    }

                    unable.Add(pos);
                }
            }
            
            #endregion
            var maskedList = masked.Except(targetable).ToList();
            maskedList =  maskedList.Except(unable).ToList();
            
            return new BattleActionPreviewData(targetable, unable, maskedList);
        }
    }
}
