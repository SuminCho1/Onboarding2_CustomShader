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
    
    public enum FilterMode
    {
        PCF2X2, PCF3X3, PCF5X5, PCF7X7
    }
    
    [System.Serializable]
    public struct Directional
    {
        public enum CascadeBlendMode
        {
            Hard, Soft, Dither
        }
        
        public TEXTURESIZE AtlasSize;
        public FilterMode Filter;
        
        [Range(1, 4)] public int CascadeCount;

        [Range(0f, 1f)] public float CascadeRatio1, CascadeRatio2, CascadeRatio3;

        [Range(0.001f, 1f)] public float CascadeFade;

        public CascadeBlendMode CascadeBlend;
        
        public Vector3 CascadeRatios => 
            new Vector3(CascadeRatio1, CascadeRatio2, 
                CascadeRatio3);
    }

    public Directional DirectionalData = new Directional
    {
        AtlasSize = TEXTURESIZE._1024,
        Filter = FilterMode.PCF2X2,
        CascadeCount = 4,
        CascadeRatio1 = 0.1f,
        CascadeRatio2 = 0.25f,
        CascadeRatio3 = 0.5f,
        CascadeFade = 0.1f,
        CascadeBlend = Directional.CascadeBlendMode.Hard
    };
    
    [Min(0.001f)] public float MaxDistance = 100f;

    [Range(0.001f, 1f)] public float DistanceFade = 0.1f;
}
