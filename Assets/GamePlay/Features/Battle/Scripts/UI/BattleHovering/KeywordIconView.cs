using GamePlay.Common.Scripts.Keyword;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.UI.BattleHovering
{
    public sealed class KeywordIconView : MonoBehaviour
    {
        private Image _icon;
        private TextMeshProUGUI _name;
        private TextMeshProUGUI _remainingTurns;

        public static KeywordIconView Create(Transform parent)
        {
            GameObject root = new("KeywordIcon", typeof(RectTransform), typeof(KeywordIconView));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(parent, false);
            rootRect.sizeDelta = new Vector2(52f, 64f);

            KeywordIconView view = root.GetComponent<KeywordIconView>();
            view.BuildVisuals();
            return view;
        }

        public void SetData(KeywordSnapshot snapshot, Sprite sprite)
        {
            _icon.sprite = sprite;
            _icon.color = sprite ? Color.white : new Color(0.35f, 0.18f, 0.55f, 0.9f);
            _name.text = string.IsNullOrWhiteSpace(snapshot.Name)
                ? snapshot.KeywordType.ToString()
                : snapshot.Name;
            _remainingTurns.text = snapshot.RemainingTurns.ToString();
        }

        private void BuildVisuals()
        {
            GameObject iconObject = new("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(transform, false);
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(48f, 48f);
            _icon = iconObject.GetComponent<Image>();
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;

            _name = CreateText("Name", transform, 11f, TextAlignmentOptions.Center);
            RectTransform nameRect = _name.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = Vector2.zero;
            nameRect.sizeDelta = new Vector2(0f, 16f);

            _remainingTurns = CreateText("RemainingTurns", iconRect, 22f, TextAlignmentOptions.BottomRight);
            RectTransform turnsRect = _remainingTurns.rectTransform;
            turnsRect.anchorMin = Vector2.zero;
            turnsRect.anchorMax = Vector2.one;
            turnsRect.offsetMin = new Vector2(2f, 1f);
            turnsRect.offsetMax = new Vector2(-2f, -1f);
            _remainingTurns.fontStyle = FontStyles.Bold;
            _remainingTurns.outlineWidth = 0.25f;
            _remainingTurns.outlineColor = Color.black;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            return text;
        }
    }
}
