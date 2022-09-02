using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject
{
    [SerializeField] private Shader _shader = default;

    [System.NonSerialized] private Material _material;

    [System.Serializable]
    public struct BloomSettings
    {
        [Range(0f, 16f)] public int MaxIterations;
        [Min(1f)] public int DownscaleLimit;
    }

    [SerializeField] private BloomSettings BloomSetting = default;

    public BloomSettings Bloom => BloomSetting;
    
    public Material Material
    {
        get
        {
            if (_material == null && _shader != null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
            }

            return _material;
        }
    }
}
