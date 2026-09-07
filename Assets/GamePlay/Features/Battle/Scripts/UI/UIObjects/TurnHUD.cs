using System.Collections.Generic;
using UnityEngine;
using ResourceManager = Core.Scripts.Managers.ResourceManager;

namespace GamePlay.Features.Battle.Scripts.UI.UIObjects
{
    public class TurnHUD : MonoBehaviour
    {
        [SerializeField] private List<TurnPortrait> turns;
        [SerializeField] private int currentIndex;
        
        public void MoveToNextTurn()
        {
            if (turns.Count == 0)
                return;

            if (currentIndex >= 0 && currentIndex < turns.Count)
                turns[currentIndex].SetCurrentTurn(false);

            currentIndex = (currentIndex + 1) % turns.Count;
            turns[currentIndex].SetCurrentTurn(true);
        }

        public void OnRoundStart()
        {
            currentIndex = -1;
        }

        public void AddToTurnList(TurnPortrait turn)
        {
            turns.Add(turn);
        }

        public void ClearList()
        {
            foreach (TurnPortrait turn in turns)
            {
                ResourceManager.Instance.ReleaseInstance(turn.gameObject);
            }
            turns.Clear();
        }

        public void FindDeadCharacter(long uid)
        {
            TurnPortrait portrait = turns.Find(x => x.CharUID == uid);
            if(!portrait) return;
            portrait.OnCharacterDie();
        }
    }
}
