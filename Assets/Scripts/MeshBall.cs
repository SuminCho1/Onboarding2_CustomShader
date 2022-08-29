using System;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class MeshBall : MonoBehaviour
{
    private static int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static int MetallicId = Shader.PropertyToID("_Metallic");
    private static int SmoothnessId = Shader.PropertyToID("_Smoothness");
    
    [SerializeField] private Mesh mesh = default;
    [SerializeField] private Material material = default;

    private Matrix4x4[] _matrices = new Matrix4x4[1023];
    private Vector4[] _baseColors = new Vector4[1023];
    private float[] _metallic = new float[1023];
    private float[] _smoothness = new float[1023];
    
    private MaterialPropertyBlock block;

    private void Awake()
    {
        for (int i = 0; i < _matrices.Length; ++i)
        {
            var quaternion = Quaternion.Euler(
                Random.value * 360f, Random.value * 360f, Random.value * 360f
            );
            
            _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10f, quaternion, Vector3.one * Random.Range(0.5f, 1.5f));

            _baseColors[i] = 
                new Vector4(Random.value, Random.value, Random.value, Random.Range(0.5f, 1f));

            _metallic[i] = Random.value < 0.25f ? 1f : 0f;
            _smoothness[i] = Random.Range(0.05f, 0.95f);
        }
    }

    private void Update()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(BaseColorId, _baseColors);
            block.SetFloatArray(MetallicId, _metallic);
            block.SetFloatArray(SmoothnessId, _smoothness);
        }
        Graphics.DrawMeshInstanced(mesh, 0, material, _matrices, 1023, block);
    }
}
