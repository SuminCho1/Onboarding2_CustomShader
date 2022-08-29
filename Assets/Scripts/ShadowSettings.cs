using UnityEngine;

[System.Serializable]
public class ShadowSettings
{
    public enum TEXTURESIZE
    {
        _256 = 256, _512 = 512, 
        _1024 = 1024, _2048 = 2048, 
        _4096 = 4096, _8192 = 8192
    }

    [System.Serializable]
    public struct Directional
    {
        public TEXTURESIZE AtlasSize;
    }

    public Directional DirectionalTexture = new Directional
    {
        AtlasSize = TEXTURESIZE._1024
    };
    
    [Min(0f)] public float MaxDistance = 100f;
}
