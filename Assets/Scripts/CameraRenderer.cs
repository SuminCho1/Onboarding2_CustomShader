using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer
{
    private const string BufferName = "Render Camera";
    
    private static ShaderTagId _unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    private static ShaderTagId _litShaderTagId = new ShaderTagId("CustomLit");
    
    private CommandBuffer _buffer = new CommandBuffer { name = BufferName };
    
    private ScriptableRenderContext _context;
    private Camera _camera;

    private CullingResults _cullingResults;

    private Lighting _lighting = new Lighting();
    
    public void Render(ScriptableRenderContext context, Camera camera, 
        bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
    {
        _context = context;
        _camera = camera;

        PrepareForSceneWindow();
        
        if (!Cull(shadowSettings.MaxDistance))
            return;
        
        _buffer.BeginSample(BufferName);
        ExecuteBuffer();
        _lighting.Setup(context, _cullingResults, shadowSettings);
        _buffer.EndSample(BufferName);
        
        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawGizmos();
        _lighting.Cleanup();
        Submit();
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
            enableInstancing = useGPUInstancing
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