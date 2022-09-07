using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

public class ComputeShaderRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private ComputeShader _computeShader;
    [SerializeField] private string _kernelName;
    
    private class ComputeShaderPass : ScriptableRenderPass
    {
        private const string ProfilerTag = "Compute Shader Pass";

        private ComputeShader _computeShader;

        private RenderTargetIdentifier _renderTargetIdentifier;

        private int _renderTargetId;
        private readonly int _mainKernel;

        private Vector2Int _textureSize = new(256, 256);

        public ComputeShaderPass(ComputeShader shader, int renderTargetId, int kernelId)
        {
            _computeShader = shader;
            _renderTargetId = renderTargetId;
            _mainKernel = kernelId;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            renderPassEvent = RenderPassEvent.AfterRendering;
            
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            
            //컴퓨트 쉐이더를 작동시키는 플래그, 그러나 안티앨리어싱이 비활성화 되어야한다.
            cameraTargetDescriptor.enableRandomWrite = true;
            cmd.GetTemporaryRT(_renderTargetId, cameraTargetDescriptor);
            _renderTargetIdentifier = new RenderTargetIdentifier(_renderTargetId);

            _textureSize.x = cameraTargetDescriptor.width;
            _textureSize.y = cameraTargetDescriptor.height;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isSceneViewCamera)
                return;
            
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
            {
                _computeShader.GetKernelThreadGroupSizes(_mainKernel, out uint xGroupSize, out uint yGroupSize, out _);
                
                //Copy texture
                cmd.Blit(renderingData.cameraData.targetTexture, _renderTargetIdentifier);
                cmd.SetComputeTextureParam(_computeShader, _mainKernel, _renderTargetId, _renderTargetIdentifier);
                cmd.SetComputeFloatParam(_computeShader, "Resolution", _textureSize.x);
                //Run shader
                cmd.DispatchCompute(_computeShader, _mainKernel, _textureSize.x / (int)xGroupSize, _textureSize.y / (int)yGroupSize, 1);
                
                //Get shader result
                cmd.Blit(_renderTargetIdentifier, renderingData.cameraData.renderer.cameraColorTarget);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_renderTargetId);
        }
    }

    private ScriptableRenderPass _pass;
    
    public override void Create()
    {
        if (_computeShader == null)
            return;
        
        int renderTargetId = Shader.PropertyToID("Result");
        int kernelId = _computeShader.FindKernel(_kernelName);
        _pass = new ComputeShaderPass(_computeShader, renderTargetId, kernelId);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_computeShader == null)
            return;
        
        renderer.EnqueuePass(_pass);
    }
}