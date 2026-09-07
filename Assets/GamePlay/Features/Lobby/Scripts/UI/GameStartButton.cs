using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Features.Lobby.Scripts.UI
{
    public class GameStartButton : MonoBehaviour
    {
        private Button _button;
        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnButtonClick);
        }

        private void OnButtonClick()
        {
        }
    }
}