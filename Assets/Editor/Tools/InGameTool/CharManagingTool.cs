#if UNITY_EDITOR

using GamePlay.Features.Battle.Scripts;
using GamePlay.Features.Battle.Scripts.Unit;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AngelBeat
{
    // 이걸 왜 만들었는가 : 
    // 하이어라키에서 Delete로 캐릭터를 없애면 NRE가 왕창 뜰거기 때문.
    // 안전한 테스팅을 위한 게임 로직으로 캐릭터를 없애는 기능임
    public class CharManagingTool : EditorWindow
    {
        private List<CharBase> characterList = new();
        private int VictimOrder = -1;

        Vector2 scrollPos;

        [MenuItem("InGame Tool/캐릭터 생성 및 삭제")]
        public static void ShowWindow()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("Unity 플레이 후 사용 바랍니다.");
                return;
            }
            GetWindow(typeof(CharManagingTool), false, "Character Managing Tool");

        }

        private void OnEnable()
        {
            if (EditorApplication.isPlaying)
            {
                UpdateCharacterList();
            }
        }

        private void UpdateCharacterList() => characterList = BattleCharManager.Instance.GetCurrentCharacters();

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("Unity 플레이 후 사용 바랍니다.");
                Close();
            }
                        
            EditorGUILayout.HelpBox("비전투 시 사용을 권장합니다.", MessageType.Warning);
            
            EditorGUILayout.Space();

            string[] characterNames = characterList.Select(c => $"{c.GetID()} - {c.UnitName}").ToArray();

            EditorGUILayout.LabelField("삭제할 캐릭터 선택", EditorStyles.boldLabel);
            VictimOrder = EditorGUILayout.Popup("캐릭터 선택", VictimOrder, characterNames);

            var victim = VictimOrder == -1 ? null : characterList[VictimOrder];
            string guide = victim == null ? "삭제할 캐릭터를 선택해주세요." : $"{victim.GetID()}번 캐릭터 {victim.name}을 삭제합니다.";

            GUIStyle ButtonStyle = new(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30,
                alignment = TextAnchor.MiddleCenter
            };

            if (GUILayout.Button(guide, ButtonStyle, GUILayout.ExpandWidth(true)) &&
                (victim == true)&& VictimOrder > -1)
            {
                victim.CharDead();
                UpdateCharacterList();
            }

        }

    }
}

#endif