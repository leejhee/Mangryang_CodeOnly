using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    private void Start()
    {
        PlayTutorial_1();
    }
    
    private async void PlayTutorial_1()
    {
        await NovelManager.InitAsync();

        NovelManager.Instance.PlayScript("6_5");


    }
}
