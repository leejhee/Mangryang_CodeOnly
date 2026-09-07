using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResourceManager = Core.Scripts.Managers.ResourceManager;

namespace GamePlay.Features.Battle.Scripts.UI.CharacterInfoPopup
{
    public class PortraitPanel : MonoBehaviour
    {
        private Dictionary<string, Sprite> _partyPortraits = new Dictionary<string, Sprite>();
        
        
        public async UniTask PreloadPortraits(List<CharacterModel> modelList)
        {
            foreach (CharacterModel model in modelList)
            {
                UniTask<Sprite> handle = ResourceManager.Instance.LoadAsync<Sprite>(model.LdRoot);
                Sprite sprite = await handle;

                _partyPortraits[model.Name] = sprite;
                
            }
        }

        public void ReleaseAllPortraits()
        {
            foreach (Sprite sprite in _partyPortraits.Values)
            {
                ResourceManager.Instance.Release(sprite);
            }
        }
        
        [SerializeField] private Image characterPortrait;
        [SerializeField] private TMP_Text characterName;
        [SerializeField] private Image characterClass;
        public async void SetPortraitPanel(CharacterModel model)
        {
            
            characterPortrait.sprite = _partyPortraits[model.Name];
            characterName.text = model.Name;
        }
    }
}
