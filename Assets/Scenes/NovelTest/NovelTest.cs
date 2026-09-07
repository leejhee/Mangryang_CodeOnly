using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class NovelTest : MonoBehaviour
{

    public string scriptName;
    // Start is called before the first frame update
    void Start()
    {
        PlayTutorial_1();
    }

    private async void PlayTutorial_1()
    {
        await NovelManager.InitAsync();
        NovelManager.Instance.PlayScript(scriptName);

    }
}
