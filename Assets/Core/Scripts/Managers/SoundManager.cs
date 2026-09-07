using Core.Scripts.Foundation;
using Core.Scripts.Foundation.Singleton;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;
using static Core.Scripts.Foundation.Define.SystemEnum;

namespace Core.Scripts.Managers
{
    public class SoundManager : SingletonObject<SoundManager>
    {
        #region 생성자
        private SoundManager() { }
        #endregion

        AudioSource[] _audioSources = new AudioSource[(int)Sound.MaxCount];
        Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();
        private bool _initialized;

        public AudioMixer audioMixer;

        public float currentBGMVolume { get; set; }
        public float currentEffectVolume { get; set; }

        public override void Init()
        {
            if (_initialized) return;

            //나중에 세이브데이터에서 받아오기
            currentBGMVolume = 1;
            currentEffectVolume = 1;


            audioMixer = Resources.Load<AudioMixer>("AudioMixer");
            AudioMixerGroup[] audioMixerGroups = audioMixer.FindMatchingGroups("Master");

            GameObject root = GameObject.Find("@Sound");
            if (root == null)
            {
                root = new GameObject { name = "@Sound" };
                Object.DontDestroyOnLoad(root);

                string[] soundNames = System.Enum.GetNames(typeof(Sound));
                for (int i = 0; i < soundNames.Length - 1; i++)
                {
                    GameObject go = new GameObject { name = soundNames[i] };
                    _audioSources[i] = go.AddComponent<AudioSource>();
                    go.transform.parent = root.transform;
                    go.GetComponent<AudioSource>().outputAudioMixerGroup = audioMixerGroups[i + 1];
                }

                _audioSources[(int)Sound.Bgm].loop = true;

            }

            _initialized = true;
        }

        public void Clear()
        {
            foreach (AudioSource audioSource in _audioSources)
            {
                audioSource.clip = null;
                audioSource.Stop();
            }
            _audioClips.Clear();
        }

        public async UniTask PlayByAddresables(string address)
        {
            
        }
        public async UniTask<AudioClip> LoadAudioClipByAddressables(string address)
        {
            AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(address);
            return await handle.Task;
        }
        
        public void Play(AudioClip audioClip, Sound type = Sound.Effect, float pitch = 1.0f)
        {
            if (audioClip == null)
            {
                return;
            }
            if (type == Sound.Bgm)
            {
                AudioSource audioSource = _audioSources[(int)Sound.Bgm];
                if (audioSource.isPlaying)
                    audioSource.Stop();
                audioSource.pitch = pitch;
                audioSource.clip = audioClip;
                audioSource.Play();
            }
            else
            {
                AudioSource audioSource = _audioSources[(int)Sound.Effect];
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(audioClip);
            }
        }
        
        public void Play(string path, Sound type = Sound.Effect, float pitch = 1.0f)
        {
            AudioClip audioClip = GetOrAddAudioClip(path, type);
            Play(audioClip, type, pitch);
        }

        AudioClip GetOrAddAudioClip(string path, Sound type = Sound.Effect)
        {
            //if (path.Contains("Sounds/") == false)
            //    path = $"Sounds/{path}";
            AudioClip audioClip = null;

            if (type == Sound.Bgm)
            {
                audioClip = Core.Scripts.Managers.ResourceManager.Instance.Load<AudioClip>(path);
            }
            else
            {
                if (_audioClips.TryGetValue(path, out audioClip) == false)
                {
                    audioClip = Core.Scripts.Managers.ResourceManager.Instance.Load<AudioClip>(path);
                    _audioClips.Add(path, audioClip);
                }
            }

            if (audioClip == null)
                Debug.Log($"AudioClip Missing {path}");

            return audioClip;
        }
        public bool isBGMPlaying()
        {
            return _audioSources[(int)Sound.Bgm].isPlaying;
        }
        public void StopBGM()
        {
            AudioSource bgmSource = _audioSources[(int)Sound.Bgm];
            if (bgmSource.isPlaying)
            {
                bgmSource.Stop();
                bgmSource.clip = null; // 필요 없으면 이 줄은 빼도 돼!
            }
        }
    }
}
