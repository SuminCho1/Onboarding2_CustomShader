using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class PostFXStack
{
    private enum Pass
    {
        BloomVertical,
        BloomHorizontal,
        Copy, 
    }
    
    private const string BufferName = "Post FX";
    private const int MaxBloomPyramidLevels = 16;
    
    private CommandBuffer _buffer = new CommandBuffer
    {
        name = BufferName
    };

    private ScriptableRenderContext _context;
    private Camera _camera;
    private PostFXSettings _settings;

    private int _fxSourceId = Shader.PropertyToID("_PostFXSource");

    private int _bloomPyramidId;
    
    public bool IsActive => _settings != null;

    public PostFXStack()
    {
        _bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 1; i < MaxBloomPyramidLevels * 2; ++i)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }
    
    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings)
    {
        _context = context;
        _camera = camera;
        _settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        DoBloom(sourceId);
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    private void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        _buffer.SetGlobalTexture(_fxSourceId, from);
        _buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _buffer.DrawProcedural(Matrix4x4.identity, _settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    private void ApplySceneViewState()
    {
        if (_camera.cameraType == CameraType.SceneView &&
            !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
            _settings = null;
    }

    private void DoBloom(int sourceId)
    {
        _buffer.BeginSample("Bloom");

        var bloom = _settings.Bloom;
        
        int width = _camera.pixelWidth / 2;
        int height = _camera.pixelHeight / 2;

        RenderTextureFormat format = RenderTextureFormat.Default;
        
        int fromId = sourceId, toId = _bloomPyramidId + 1;

        int i;
        for (i = 0; i < bloom.MaxIterations; ++i)
        {
            if (height < bloom.DownscaleLimit || 
                width < bloom.DownscaleLimit)
                break;

            int midId = toId - 1;
            _buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            
            _buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);

            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }
        
        Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);

        for (i -= 1; i >= 0; --i)
        {
            _buffer.ReleaseTemporaryRT(fromId);
            _buffer.ReleaseTemporaryRT(fromId - 1);
            fromId -= 2;
        }
        
        _buffer.EndSample("Bloom");
    }
}