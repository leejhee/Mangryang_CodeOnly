//using System;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

//public class MapNodePrefab : UI_Base
//{
//    BaseMapNodeData _data;

//    public enum Images
//    {
//        NodeImage
//    }

//    public enum Buttons
//    {
//        NodeButton,
//    }

//    public override void Init()
//    {
//        Bind<Button>(typeof(Buttons));
//        Bind<Image>(typeof(Images));
//        BindEvent(Get<Button>((int)Buttons.NodeButton).gameObject, OnClickPrefab);
//    }

//    public void SetNodeInfo(BaseMapNodeData nodeData)
//    {
//        _data = nodeData;
//        Image nodeImage = Get<Image>((int)Images.NodeImage);
//        nodeImage.sprite = _data.nodeSprite;
//    }

//    // 현재 클릭으로 invoke됨. 
//    public void OnClickPrefab(PointerEventData evt)
//    {
//        //StageManager.Instance.ProceedStage(_data.nodeType);
//    }

//}