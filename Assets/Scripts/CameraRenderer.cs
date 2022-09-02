using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer
{
    private const string BufferName = "Render Camera";
    
    private static ShaderTagId _unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    private static ShaderTagId _litShaderTagId = new ShaderTagId("CustomLit");

    private static int FrameBufferId = Shader.PropertyToID("_CameraFrameBuffer");

    private CommandBuffer _buffer = new CommandBuffer { name = BufferName };
    private ScriptableRenderContext _context;
    private Camera _camera;
    private CullingResults _cullingResults;
    private Lighting _lighting = new Lighting();
    private PostFXStack _postFXStack = new PostFXStack();
    
    public void Render(ScriptableRenderContext context, Camera camera, 
        bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings,
        PostFXSettings postFXSettings)
    {
        _context = context;
        _camera = camera;

        PrepareForSceneWindow();
        
        if (!Cull(shadowSettings.MaxDistance))
            return;
        
        _buffer.BeginSample(BufferName);
        ExecuteBuffer();
        _lighting.Setup(context, _cullingResults, shadowSettings);
        _postFXStack.Setup(context, camera, postFXSettings);
        _buffer.EndSample(BufferName);
        
        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        
        DrawGizmosBeforeFX();

        CameraClearFlags flags = _camera.clearFlags;
        if (_postFXStack.IsActive)
        {
            if (flags > CameraClearFlags.Color)
                flags = CameraClearFlags.Color;
            
            _postFXStack.Render(FrameBufferId);
        }
        
        DrawGizmosAfterFX();
        
        Cleanup();

        Submit();
    }

    private void Cleanup()
    {
        _lighting.Cleanup();
        if(_postFXStack.IsActive)
            _buffer.ReleaseTemporaryRT(FrameBufferId);
    }

    private bool Cull(float maxShadowDistance)
    {
        if (_camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, _camera.farClipPlane);
            _cullingResults = _context.Cull(ref p);
            return true;
        }

        return false;
    }

    private void Setup()
    {
        _context.SetupCameraProperties(_camera);

        if (_postFXStack.IsActive)
        {
            _buffer.GetTemporaryRT(FrameBufferId, _camera.pixelWidth,
                _camera.pixelHeight, 32, FilterMode.Bilinear,
                RenderTextureFormat.Default);
            
            _buffer.SetRenderTarget(FrameBufferId, 
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }
        
        _buffer.ClearRenderTarget(true, true, Color.clear);
        _buffer.BeginSample(BufferName);
        ExecuteBuffer();
    }

    private void Submit()
    {
        _buffer.EndSample(BufferName);
        ExecuteBuffer();
        _context.Submit();
    }

    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        var sortingSettings = new SortingSettings();
        var drawingSettings = new DrawingSettings(_unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.ReflectionProbes |
                            PerObjectData.Lightmaps | PerObjectData.LightProbe |
                            PerObjectData.LightProbeProxyVolume
        };
        drawingSettings.SetShaderPassName(1, _litShaderTagId);
        
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        
        _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        
        _context.DrawSkybox(_camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        
        _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
            _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }
    }

    private void DrawGizmosBeforeFX()
    {
        if (Handles.ShouldRenderGizmos())
        {
            _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
        }
    }

    private void DrawGizmosAfterFX()
    {
        if (Handles.ShouldRenderGizmos())
        {
            _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }
    }

    private void PrepareForSceneWindow()
    {
        if (_camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
        }
    }
    
    private void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
}