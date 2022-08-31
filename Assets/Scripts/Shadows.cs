using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private const int MaxShadowedDirectionalLightCount = 4;
    private const int MaxCascades = 4;
    
    private const string BufferName = "Shadows";

    private static int DirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    private static int DirShadowMatricesId = 
        Shader.PropertyToID("_DirectionalShadowMatrices");
    private static int CascadeCountId = Shader.PropertyToID("_CascadeCount");
    private static int CascadeCullingSpheresId = 
        Shader.PropertyToID("_CascadeCullingSpheres");

    private static int CascadeDataId = Shader.PropertyToID("_CascadeData");
    private static int ShadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
    private static int ShadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
    
    private static Matrix4x4[] DirShadowMatrices = 
        new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades];
    private static Vector4[] CascadeCullingSpheres = new Vector4[MaxCascades];
    private static Vector4[] CascadeData = new Vector4[MaxCascades];

    private static string[] DirectionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };

    private static string[] CascadeBlendKeywords =
    {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };
    
    private struct ShadowedDirectionalLight
    {
        public int VisibleLightIndex;
        public float SlopeScaleBias;
        public float NearPlaneOffset;
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

    public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (_shadowedDirectionalLightCount < MaxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0f &&
            _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            _shadowedDirectionalLights[_shadowedDirectionalLightCount] =
                new ShadowedDirectionalLight
                {
                    VisibleLightIndex = visibleLightIndex,
                    SlopeScaleBias = light.shadowBias,
                    NearPlaneOffset = light.shadowNearPlane
                };

            return new Vector3(
                light.shadowStrength, 
                _settings.DirectionalData.CascadeCount * _shadowedDirectionalLightCount++,
                light.shadowNormalBias);
        }

        return Vector3.zero;
    }

    private void SetKeywords(string[] keywords, int enabledIndex)
    {
        for (int i = 0; i < keywords.Length; ++i)
        {
            if(i == enabledIndex)
                _buffer.EnableShaderKeyword(keywords[i]);
            else
                _buffer.DisableShaderKeyword(keywords[i]);
        }
    }
    private void ExecuteBuffer()
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
        int atlasSize = (int)_settings.DirectionalData.AtlasSize;
        _buffer.GetTemporaryRT(DirShadowAtlasId, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        _buffer.SetRenderTarget(DirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _buffer.ClearRenderTarget(true, false, Color.clear);
        
        _buffer.BeginSample(BufferName);
        ExecuteBuffer();

        int tiles = _shadowedDirectionalLightCount * _settings.DirectionalData.CascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < _shadowedDirectionalLightCount; ++i)
            RenderDirectionalShadows(i, split, tileSize);

        _buffer.SetGlobalInt(CascadeCountId, _settings.DirectionalData.CascadeCount);
        _buffer.SetGlobalVectorArray(CascadeCullingSpheresId, CascadeCullingSpheres);
        _buffer.SetGlobalVectorArray(CascadeDataId, CascadeData);
        _buffer.SetGlobalMatrixArray(DirShadowMatricesId, DirShadowMatrices);

        float f = 1f - _settings.DirectionalData.CascadeFade;
        _buffer.SetGlobalVector(ShadowDistanceFadeId,
            new Vector4(1f / _settings.MaxDistance, 1f / _settings.DistanceFade, 1f / (1f - f * f)));
        
        SetKeywords(DirectionalFilterKeywords, (int)_settings.DirectionalData.Filter - 1);
        SetKeywords(CascadeBlendKeywords, (int)_settings.DirectionalData.CascadeBlend - 1);
        
        _buffer.SetGlobalVector(ShadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
        
        _buffer.EndSample(BufferName);
        ExecuteBuffer();
    }

    private void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
        var shadowSettings = 
            new ShadowDrawingSettings(_cullingResults, light.VisibleLightIndex);

        int cascadeCount = _settings.DirectionalData.CascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = _settings.DirectionalData.CascadeRatios;

        float cullingFactor = 
            Mathf.Max(0f, 0.8f - _settings.DirectionalData.CascadeFade);
        
        for (int i = 0; i < cascadeCount; ++i)
        {
            _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.VisibleLightIndex, i, cascadeCount,
                ratios, tileSize, light.NearPlaneOffset, 
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData);

            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowSettings.splitData = splitData;

            if (index == 0)
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            
            int tileIndex = tileOffset + i;
            SetTileViewport(index, split, tileSize);
        
            DirShadowMatrices[tileIndex] = 
                ConvertToAtlasMatrix(projectionMatrix * viewMatrix, 
                    SetTileViewport(tileIndex, split, tileSize), split);

            _buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            _buffer.SetGlobalDepthBias(0f, light.SlopeScaleBias);
            
            ExecuteBuffer();
        
            //ShadowCaster 패스를 가진 머티리얼을 렌더링함
            _context.DrawShadows(ref shadowSettings);
            _buffer.SetGlobalDepthBias(0f, 0f);
        }
    }

    private void SetCascadeData(int index, Vector4 cullingSphere, int tileSize)
    {
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)_settings.DirectionalData.Filter + 1f);
        
        //캐스케이드의 컬링 영역을 샘플링하는 것을 방지한다
        cullingSphere.w -= filterSize;
        cullingSphere.w *= cullingSphere.w;
        CascadeCullingSpheres[index] = cullingSphere;
        CascadeData[index] = 
            new Vector4(1f/ cullingSphere.w, filterSize * 1.4142136f);
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
