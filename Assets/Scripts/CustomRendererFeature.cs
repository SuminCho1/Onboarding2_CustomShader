using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CustomRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PassSettings
    {
        public RenderPassEvent RenderPassEvent = 
            RenderPassEvent.AfterRenderingTransparents;
        public Material Material;
    }

    private CustomRenderPass _pass;
    
    public PassSettings Settings = new();
    
    public override void Create()
    {
        _pass = new CustomRenderPass(Settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_pass);
    }
}