using System;

namespace Core.Scripts.GameSave
{
    /// <summary>
    /// 게임 전역 설정 관련 데이터 클래스
    /// <remarks>(전역 아닌 설정은 없는것으로 간주)</remarks>
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public SoundSettings Sound = new();
        public GraphicSettings Graphic = new();
        public string Language = "Korean";
    }
    
    [Serializable]
    public class SoundSettings
    {
        public float MasterVolume = 1.0f;
        public bool MasterMute = false;
        
        public float BGMVolume = 1.0f;
        public bool BGMMute = false;
        
        public float SfxVolume = 1.0f;
        public bool SfxMute = false;
    }
    
    [Serializable]
    public class GraphicSettings
    {
        public bool Fullscreen = true;
        public int ResolutionIndex = 0;
    }
}