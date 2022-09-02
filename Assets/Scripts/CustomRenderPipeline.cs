using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    private readonly CameraRenderer _renderer = new CameraRenderer();
    private readonly bool _useDynamicBatching;
    private readonly bool _useGPUInstancing;
    private ShadowSettings _shadowSettings;
    private PostFXSettings _postFxSettings;
    
    public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, 
        bool useSRPBatcher, ShadowSettings shadowSettings, PostFXSettings postFxSettings)
    {
        _useDynamicBatching = useDynamicBatching;
        _useGPUInstancing = useGPUInstancing;
        _shadowSettings = shadowSettings;
        _postFxSettings = postFxSettings;
        
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
    }
    
    //렌더링에 필요하지 않은 오브젝트를 컬링한다. 모든 카메라에 해당한다
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            _renderer.Render(context, camera, _useDynamicBatching, _useGPUInstancing, _shadowSettings, _postFxSettings);
        }
    }
}
