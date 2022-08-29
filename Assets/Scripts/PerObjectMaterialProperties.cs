using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    private static int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static int CutoffId = Shader.PropertyToID("_Cutoff");
    private static int MetallicId = Shader.PropertyToID("_Metallic");
    private static int SmoothnessId = Shader.PropertyToID("_Smoothness");
    
    private static MaterialPropertyBlock block;
    
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float cutoff = 0.5f, metallic = 0f, smoothness = 0.5f;
    
    private Renderer _renderer;

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (block == null)
            block = new MaterialPropertyBlock();
        
        block.SetColor(BaseColorId, baseColor);
        block.SetFloat(CutoffId, cutoff);
        block.SetFloat(MetallicId, metallic);
        block.SetFloat(SmoothnessId, smoothness);
        
        if(_renderer == null)
            _renderer = GetComponent<Renderer>();
        
        _renderer.SetPropertyBlock(block);
    }
}
