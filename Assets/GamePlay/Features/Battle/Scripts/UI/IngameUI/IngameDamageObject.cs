using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using TMPro;
using UnityEngine.Serialization;

public class IngameDamageObject : MonoBehaviour
{
    [SerializeField] private float duration;
    [SerializeField] private float floatingHeight;
    [SerializeField] private TMP_Text damageText;

    public async void Init(long damage)
    {
        var renderer = GetComponent<Renderer>();
        renderer.sortingOrder = 25;
        
        // 텍스트 바꿔주고
        damageText.text = damage.ToString();
        
        // 살아있는 시간, 올라가는 위치
        float curDuration = 0f;
        
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * floatingHeight;
        
        while (curDuration < duration)
        {
            curDuration += Time.deltaTime;
            float t = curDuration / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);

            await UniTask.Yield();
        }
        Addressables.Release(gameObject);
    }
}
