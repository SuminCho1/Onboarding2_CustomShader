using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    private const int MaxDirLightCount = 4;
    private const string BufferName = "Lighting";
    
    private static int DirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static int DirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static int DirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    private static int DirLightShadowDataId =
        Shader.PropertyToID("_DirectionalLightShadowData");
    
    private static Vector4[] DirLightColors = new Vector4[MaxDirLightCount];
    private static Vector4[] DirLightDirections = new Vector4[MaxDirLightCount];
    private static Vector4[] DirLightShadowData = new Vector4[MaxDirLightCount];
    
    private CommandBuffer _buffer = new CommandBuffer
    {
        name = BufferName
    };

    private CullingResults _cullingResults;
    private Shadows _shadows = new Shadows();
    
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults,
        ShadowSettings shadowSettings)
    {
        _cullingResults = cullingResults;
        _buffer.BeginSample(BufferName);
        {
            _shadows.Setup(context, cullingResults, shadowSettings);
            SetupLights();
            _shadows.Render();
        }
        _buffer.EndSample(BufferName);
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    private void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;

        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; ++i)
        {
            var visibleLight = visibleLights[i];
            if (visibleLight.lightType == LightType.Directional)
            {
                SetupDirectionalLight(dirLightCount++, ref visibleLight);
                if (dirLightCount >= MaxDirLightCount)
                    break;
            }
        }
        
        _buffer.SetGlobalInt(DirLightCountId, visibleLights.Length);
        _buffer.SetGlobalVectorArray(DirLightColorsId, DirLightColors);
        _buffer.SetGlobalVectorArray(DirLightDirectionsId, DirLightDirections);
        _buffer.SetGlobalVectorArray(DirLightShadowDataId, DirLightShadowData);
    }

    private void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        DirLightColors[index] = visibleLight.finalColor;
        DirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        DirLightShadowData[index] = _shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }

    public void Cleanup()
    {
        _shadows.Cleanup();
    }
}