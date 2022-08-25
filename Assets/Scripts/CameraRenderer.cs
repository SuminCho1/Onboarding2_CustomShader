using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer
{
    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    private const string bufferName = "Render Camera";
    
    //커스텀 렌더 파이프라인을 사용할 경우, 콘텍스트를 통하여 상태 업데이트를 스케쥴하고 제출하며 명령을 GPU에 그린다
    //글로벌 쉐이더 속성, 렌더타겟 변경, 컴퓨트 쉐이더 전달 등의 역할을 한다.
    //Submit을 하여 실제로 렌더 루프를 실행한다.
    private ScriptableRenderContext context;
    private Camera camera;
    
    //자주 쓰이는 명령이 아니라면 커맨드 버퍼에 따로 추가를 해주어야 한다.
    private CommandBuffer buffer = new CommandBuffer { name = bufferName };

    private CullingResults cullingResults;
    
    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        if (!Cull())
            return;
        
        Setup();
        
        DrawVisibleGeometry();
        
        Submit();
    }

    private bool Cull()
    {
        if (camera.TryGetCullingParameters(out var p))
        {
            cullingResults = context.Cull(ref p);
            return true;
        }

        return false;
    }

    private void Setup()
    {
        //View Projection 맞춰줌, 그 외의 속성들도 적용시킴
        context.SetupCameraProperties(camera);
        
        buffer.ClearRenderTarget(true, true, Color.clear);
        
        //프로파일러, 프레임 디버거에 디버그 샘플링 시작
        buffer.BeginSample(bufferName);
        
        ExecuteBuffer();
    }
    
    private void Submit()
    {
        //디버그 샘플링 종료
        buffer.EndSample(bufferName);
        
        ExecuteBuffer();
        
        //큐잉 된 리스트를 보냄
        context.Submit();
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void DrawVisibleGeometry()
    {
        //어떤 쉐이더 패스를 사용할 것인지 세팅
        var sortingSettings = new SortingSettings(camera);
        // {
        //     criteria = SortingCriteria.CommonOpaque
        // };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
        
        //어떤 렌더 큐를 사용할 것인지 세팅
        var filteringSettings = new FilteringSettings(RenderQueueRange.all);
        
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        
        context.DrawSkybox(camera);
    }
}
