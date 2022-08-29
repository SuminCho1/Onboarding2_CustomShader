using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private const int MaxShadowedDirectionalLightCount = 4;
    private const string BufferName = "Shadows";

    private static int DirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    private static int DirShadowMatricesId = 
        Shader.PropertyToID("_DirectionalShadowMatrices");

    private static Matrix4x4[] DirShadowMatrices = 
        new Matrix4x4[MaxShadowedDirectionalLightCount];
    
    private struct ShadowedDirectionalLight
    {
        public int VisibleLightIndex;
    }

    private ShadowedDirectionalLight[] _shadowedDirectionalLights =
        new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];
    
    private CommandBuffer _buffer = new CommandBuffer
    {
        name = BufferName
    };

    private ScriptableRenderContext _context;
    private CullingResults _cullingResults;
    private ShadowSettings _settings;

    private int _shadowedDirectionalLightCount;
    
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        _context = context;
        _cullingResults = cullingResults;
        _settings = shadowSettings;
        _shadowedDirectionalLightCount = 0;
    }

    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (_shadowedDirectionalLightCount < MaxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0f &&
            _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            _shadowedDirectionalLights[_shadowedDirectionalLightCount] =
                new ShadowedDirectionalLight
                {
                    VisibleLightIndex = visibleLightIndex
                };

            return new Vector2(light.shadowStrength,
                _shadowedDirectionalLightCount++);
        }

        return Vector2.zero;
    }
    
    public void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    public void Render()
    {
        if (_shadowedDirectionalLightCount > 0)
            RenderDirectionalShadows();
        else
        {
            _buffer.GetTemporaryRT(DirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            _buffer.SetRenderTarget(DirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            _buffer.ClearRenderTarget(true, false, Color.clear);
            ExecuteBuffer();
        }
    }
    
    private void RenderDirectionalShadows()
    {
        int atlasSize = (int)_settings.DirectionalTexture.AtlasSize;
        _buffer.GetTemporaryRT(DirShadowAtlasId, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        _buffer.SetRenderTarget(DirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _buffer.ClearRenderTarget(true, false, Color.clear);
        
        _buffer.BeginSample(BufferName);
        ExecuteBuffer();

        int split = _shadowedDirectionalLightCount <= 1 ? 1 : 2;
        int tileSize = atlasSize / split;
        
        for(int i =0;i<_shadowedDirectionalLightCount;++i)
            RenderDirectionalShadows(i, split, tileSize);
        
        _buffer.SetGlobalMatrixArray(DirShadowMatricesId, DirShadowMatrices);
        
        _buffer.EndSample(BufferName);
        ExecuteBuffer();
    }

    private void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
        var shadowSettings = 
            new ShadowDrawingSettings(_cullingResults, light.VisibleLightIndex);

        _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            light.VisibleLightIndex, 0, 1,
            Vector3.zero, tileSize, 0f, 
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
            out ShadowSplitData splitData);

        shadowSettings.splitData = splitData;
        
        SetTileViewport(index, split, tileSize);
        
        //DirShadowMatrices[index] = projectionMatrix * viewMatrix;
        DirShadowMatrices[index] = 
            ConvertToAtlasMatrix(projectionMatrix * viewMatrix, 
                SetTileViewport(index, split, tileSize), split);

        _buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        
        //ShadowCaster 패스를 가진 머티리얼을 렌더링함
        _context.DrawShadows(ref shadowSettings);
    }
    
    private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);

        return m;
    }

    private Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        //각각의 조명에서 생긴 쉐도우맵을 겹치지 않게 한다
        Vector2 offset = new Vector2(index % split, index / split);
        _buffer.SetViewport(new Rect(
            offset.x * tileSize, offset.y * tileSize,
            tileSize, tileSize));

        return offset;
    }
    
    public void Cleanup()
    {
        _buffer.ReleaseTemporaryRT(DirShadowAtlasId);
        ExecuteBuffer();
    }
}
