using UnityEngine;
using UnityEngine.UI;

public class NoiseTexture : MonoBehaviour
{
    public ComputeShader _computeShader;
    public RenderTexture _renderTexture;

    public int _size;
    public RawImage _image;
    
    private uint _xCount;
    private uint _yCount;

    private Camera _camera;
    
    private void Start()
    {
        _camera = Camera.main;
        
        if (_renderTexture == null)
        {
            _renderTexture = new RenderTexture(_size, _size, 1);
            _renderTexture.enableRandomWrite = true;
            _renderTexture.Create();
        }

        _image.texture = _renderTexture;
        
        _computeShader.GetKernelThreadGroupSizes(0, out _xCount, out _yCount, out uint _);
        Compute(0);
    }
    
    private void Update()
    {
        Compute(1);
    }

    private void Compute(int kernelIndex)
    {
        _computeShader.SetTexture(kernelIndex, "Texture", _renderTexture);
        _computeShader.SetFloat("Size", _size);
        _computeShader.Dispatch(kernelIndex, _size / (int)_xCount, _size / (int)_yCount, 1);
    }
}
