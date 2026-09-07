// using Cysharp.Threading.Tasks;
// using Cysharp.Threading.Tasks.Triggers;
// using GamePlay.Features.Battle.Scripts;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
//
// public class tutorialCanvas : MonoBehaviour
// {
//     [SerializeField] private Button tutorialButton;
//     [SerializeField] private TMP_Text tutorialButtonText;
//     private static tutorialCanvas _instance;
//
//     private bool charMove_1 = false;
//     
//     
//     public static tutorialCanvas Instance
//     {
//         get
//         {
//             if (_instance == null)
//             {
//                 _instance =  FindObjectOfType<tutorialCanvas>();
//             }
//             
//             return _instance;
//         }
//     }
//     
//     RectTransform buttonRect;
//     RectTransform rectTransform;
//     private bool isFocusOn = false;
//     private bool isNovelEnd = false;
//     private bool firstButton = false;
//     public float buttonDistance = 0;
//     public float moveDistance = 0;
//     
//     private void Start()
//     {
//         rectTransform = GetComponent<RectTransform>();
//         buttonRect = tutorialButton.GetComponent<RectTransform>();
//         
//         
//
//     }
//
//     public async void OnClickButton()
//     {
//         if (!charMove_1)
//         {
//             Debug.Log("클릭");
//             tutorialButton.gameObject.SetActive(false);
//             Transform transform = BattleController.Instance.FocusChar.transform;
//             Vector3 vec = new Vector3(transform.position.x, transform.position.y, transform.position.z);
//             await BattleController.Instance.FocusChar.CharMove(new Vector3(vec.x + 5, vec.y , vec.z ));
//
//             await UniTask.WaitForSeconds(0.5f);
//             
//             BattleController.Instance.TurnController.ChangeTurn();
//         }
//     }
//
//     public void CharMove()
//     {
//         Transform transform = BattleController.Instance.FocusChar.transform;
//         Vector3 vec = new Vector3(transform.position.x, transform.position.y, transform.position.z);
//         BattleController.Instance.FocusChar.CharMove(new Vector3(vec.x + moveDistance, vec.y , vec.z ));
//     }
//     private void Update()
//     {
//         //if (BattleController.Instance.FocusChar != null)
//         //{
//         //    isFocusOn = true;
//         //}
//         //if (NovelManager.Instance.firstTutoEnd && !firstButton)
//         //{
//         //    isNovelEnd = true;
//         //    firstButton = true;
//         //    tutorialButton.gameObject.SetActive(true);
//         //}
//         //if (isFocusOn)
//         //{
//         //    Vector3 vec = BattleController.Instance.FocusChar.gameObject.transform.localPosition;
//         //    rectTransform.localPosition = vec;
//         //    buttonRect.localPosition = new Vector3(vec.x + buttonDistance, vec.y, vec.z);
//         //}
// //
//     }
// }
