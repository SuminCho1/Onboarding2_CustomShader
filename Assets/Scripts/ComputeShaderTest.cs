using System.Collections.Generic;
using UnityEngine;

public struct Cube
{
    public Vector3 position;
    public Color color;
}

[ExecuteInEditMode]
public class ComputeShaderTest : MonoBehaviour
{
    public ComputeShader _computeShader;
    public int _count = 10;
    public Mesh _mesh;
    public Material _material;
    public int _repetitions = 10;
    
    private List<GameObject> _objects;
    private Cube[] _data;

    private void OnGUI()
    {
        if (_objects == null)
        {
            if (GUI.Button(new Rect(0, 0, 100, 50), "Create")) 
                CreateCubes();
        }
        else
        {
            if (GUI.Button(new Rect(0, 0, 100, 50), "Random CPU"))
                OnRandomizeCPU();
            if (GUI.Button(new Rect(100, 0, 100, 50), "Random GPU"))
                OnRandomizeGPU();
        }
    }
    
    private void CreateCubes()
    {
        _objects = new List<GameObject>();
        _data = new Cube[_count * _count];

        for (int x = 0; x < _count; ++x)
        {
            for (int y = 0; y < _count; ++y)
                CreateCube(x, y);
        }
    }
    
    private void CreateCube(int x, int y)
    {
        var cube = new GameObject("Cube" + x * _count + y, typeof(MeshFilter), typeof(MeshRenderer));
        cube.GetComponent<MeshFilter>().mesh = _mesh;
        cube.GetComponent<MeshRenderer>().material = new Material(_material);
        
        cube.transform.position = new Vector3(x, y, Random.Range(-0.1f, 0.1f));
        Color color = Random.ColorHSV();
        cube.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BaseColor", color);
        
        _objects.Add(cube);

        Cube cubeData = new Cube();
        cubeData.position = cube.transform.position;
        cubeData.color = color;
        _data[x * _count + y] = cubeData;
    }

    public void OnRandomizeCPU()
    {
        for (int i = 0; i < _repetitions; ++i)
        {
            for (int c = 0; c < _objects.Count; ++c)
            {
                var obj = _objects[c];
                obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y,
                    Random.Range(-0.1f, 0.1f));
                obj.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BaseColor", Random.ColorHSV());
            }
        }
    }

    public void OnRandomizeGPU()
    {
        int colorSize = sizeof(float) * 4;
        int vector3Size = sizeof(float) * 3;
        int totalSize = colorSize + vector3Size;

        //컴퓨트 쉐이더의 구조체
        ComputeBuffer cubesBuffer = new ComputeBuffer(_data.Length, totalSize);
        cubesBuffer.SetData(_data);
        
        _computeShader.SetBuffer(0, "Cubes", cubesBuffer);
        _computeShader.SetFloat("Resolution", _data.Length);
        _computeShader.SetInt("Repetitions", _repetitions);
        _computeShader.Dispatch(0, _data.Length / 10, 1, 1);

        cubesBuffer.GetData(_data);
        
        for (int i = 0; i < _objects.Count; ++i)
        {
            var obj = _objects[i];
            Cube cube = _data[i];
            obj.transform.position = cube.position;
            obj.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BaseColor", cube.color);
        }
        
        cubesBuffer.Dispose();
    }
}
