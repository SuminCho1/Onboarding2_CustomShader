using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

public class CustomRenderPass : ScriptableRenderPass
{
    private const string ProfilerTag = "Custom Render Pass";
    
    private static int BufferId = Shader.PropertyToID("_CustomRenderBuffer");
    
    private CustomRendererFeature.PassSettings _passSettings;

    private Material _material;

    public CustomRenderPass(CustomRendererFeature.PassSettings settings)
    {
        _passSettings = settings;
        renderPassEvent = _passSettings.RenderPassEvent;

        if (_material == null)
            _material = settings.Material;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;

        descriptor.depthBufferBits = 0;

        cmd.GetTemporaryRT(BufferId, descriptor, FilterMode.Bilinear);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
        {
            Blit(cmd, ref renderingData, _material, 0);
        }
        
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (cmd == null)
            throw new ArgumentNullException("cmd");
        
        cmd.ReleaseTemporaryRT(BufferId);
    }
}