using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Keyword;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.UI.BattleHovering
{
    public sealed class KeywordBarView : MonoBehaviour
    {
        private const int MaxPerRow = 3;
        private const string CanvasName = "KeywordBarCanvas";
        private const string SkillIconAddressPrefix = "skillicon_";

        private readonly Dictionary<string, Sprite> _iconCache = new();
        private readonly List<RectTransform> _rows = new();

        private KeywordInfo _keywordInfo;
        private RectTransform _canvasRoot;
        private Canvas _canvas;
        private int _refreshVersion;

        public static void Attach(CharBase owner, KeywordInfo keywordInfo)
        {
            if (!owner || keywordInfo == null)
                return;

            KeywordBarView view = owner.GetComponentInChildren<KeywordBarView>(true);
            if (!view)
            {
                Debug.LogError(
                    $"[KeywordBarView] '{owner.name}' 프리팹에 KeywordBarView가 없습니다. " +
                    "FloatingRoot/KeywordPanel 구성을 확인해주세요.",
                    owner);
                return;
            }

            view.Bind(keywordInfo);
        }

        public void Bind(KeywordInfo keywordInfo)
        {
            if (!TryCacheCanvas())
                return;

            if (_keywordInfo == keywordInfo)
                return;

            if (_keywordInfo != null)
                _keywordInfo.Changed -= OnKeywordChanged;

            _keywordInfo = keywordInfo;
            _keywordInfo.Changed += OnKeywordChanged;
            OnKeywordChanged();
        }

        private bool TryCacheCanvas()
        {
            if (_canvasRoot && _canvas)
                return true;

            Transform existing = transform.Find(CanvasName);
            if (!existing)
            {
                Debug.LogError(
                    $"[KeywordBarView] '{name}' 아래에 {CanvasName} 오브젝트가 없습니다.",
                    this);
                return false;
            }

            RectTransform canvasRoot = existing as RectTransform;
            Canvas canvas = existing.GetComponent<Canvas>();
            if (!canvasRoot || !canvas)
            {
                Debug.LogError(
                    $"[KeywordBarView] '{existing.name}'에는 RectTransform과 Canvas가 모두 필요합니다.",
                    existing);
                return false;
            }

            _canvasRoot = canvasRoot;
            _canvas = canvas;
            return true;
        }

        private void OnKeywordChanged()
        {
            int version = ++_refreshVersion;
            RefreshAsync(version, this.GetCancellationTokenOnDestroy())
                .SuppressCancellationThrow()
                .Forget();
        }

        private async UniTask RefreshAsync(int version, CancellationToken ct)
        {
            IReadOnlyList<KeywordSnapshot> snapshots = _keywordInfo?.Snapshots;
            if (snapshots == null || snapshots.Count == 0)
            {
                ClearRows();
                if (_canvas)
                    _canvas.enabled = false;
                return;
            }

            List<Sprite> icons = new(snapshots.Count);
            foreach (KeywordSnapshot snapshot in snapshots)
            {
                ct.ThrowIfCancellationRequested();
                icons.Add(await LoadIconAsync(snapshot, ct));
            }

            if (version != _refreshVersion || ct.IsCancellationRequested)
                return;

            ClearRows();
            _canvas.enabled = true;

            int snapshotIndex = 0;
            int rowCount = Mathf.CeilToInt(snapshots.Count / (float)MaxPerRow);
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                int itemCount = Mathf.Min(MaxPerRow, snapshots.Count - snapshotIndex);
                RectTransform row = CreateRow(rowIndex, rowCount, itemCount);
                _rows.Add(row);

                for (int itemIndex = 0; itemIndex < itemCount; itemIndex++, snapshotIndex++)
                {
                    KeywordIconView iconView = KeywordIconView.Create(row);
                    iconView.SetData(snapshots[snapshotIndex], icons[snapshotIndex]);
                }
            }
        }

        private async UniTask<Sprite> LoadIconAsync(KeywordSnapshot snapshot, CancellationToken ct)
        {
            string address = GetIconAddress(snapshot.IconKey);
            if (string.IsNullOrWhiteSpace(address))
                return null;
            if (_iconCache.TryGetValue(address, out Sprite cached))
                return cached;

            try
            {
                Sprite icon = await ResourceManager.Instance.LoadAsync<Sprite>(address, ct);
                _iconCache[address] = icon;
                return icon;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[KeywordBarView] 아이콘 '{address}' 로드 실패: {exception.Message}", this);
                return null;
            }
        }

        private static string GetIconAddress(string iconKey)
        {
            if (string.IsNullOrWhiteSpace(iconKey))
                return null;

            return $"{SkillIconAddressPrefix}{iconKey}";
        }

        private RectTransform CreateRow(int rowIndex, int rowCount, int itemCount)
        {
            GameObject rowObject = new($"Row {rowIndex + 1}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            RectTransform row = rowObject.GetComponent<RectTransform>();
            row.SetParent(_canvasRoot, false);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(itemCount * 58f, 64f);
            row.anchoredPosition = new Vector2(0f, (rowCount - 1) * 34f - rowIndex * 68f);

            HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        private void ClearRows()
        {
            foreach (RectTransform row in _rows)
            {
                if (row)
                {
                    row.gameObject.SetActive(false);
                    Destroy(row.gameObject);
                }
            }
            _rows.Clear();
        }

        private void OnDestroy()
        {
            if (_keywordInfo != null)
                _keywordInfo.Changed -= OnKeywordChanged;
        }
    }

}
