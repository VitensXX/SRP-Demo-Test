using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    const string bufferName = "test Render Camera";
    CommandBuffer buffer = new CommandBuffer { name = bufferName };

    ScriptableRenderContext context;
    Camera camera;
    Lighting lighting = new Lighting();

    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings){
        this.context = context;
        this.camera = camera;

        // 设置buffer缓冲区的名字
        PrepareBuffer();
        // 在Game视图绘制的几何体也绘制到Scene视图中 ?? 不调用也可以?
        PrepareForSceneWindow();

        if(!Cull(shadowSettings.maxDistance)){
            return;
        }

        buffer.BeginSample(SampleName);
        ExecuteBuffer();

        lighting.SetUp(context, cullingResults, shadowSettings);
        buffer.EndSample(SampleName);

        Setup();

        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);

        DrawUnsupportedShaders();

        //绘制Gizmos
        DrawGizmos();

        lighting.Cleanup();
        Submit();
    }

    CullingResults cullingResults;
    //剔除
    bool Cull(float maxShadowDistance){
        ScriptableCullingParameters parameters;
        if(camera.TryGetCullingParameters(out parameters)){
            parameters.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref parameters);
            return true;
        }

        return false;
    }

    void Setup(){
        context.SetupCameraProperties(camera);
        buffer.ClearRenderTarget(true, true, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
    }

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
    //绘制可见物
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing){
        //设置绘制顺序和指定渲染相机
        var sortingSettings = new SortingSettings(camera){ criteria = SortingCriteria.CommonOpaque };

        //设置渲染的shader pass和排序模式
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
        drawingSettings.enableDynamicBatching = useDynamicBatching;
        drawingSettings.enableInstancing = useGPUInstancing;

        drawingSettings.SetShaderPassName(1, litShaderTagId);

        //设置可被渲染的类型
        var filteringSetting = new FilteringSettings(RenderQueueRange.opaque);

        //1.绘制不透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSetting);

        //2.绘制天空盒
        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        //只绘制transparent透明的物体
        filteringSetting.renderQueueRange = RenderQueueRange.transparent;
        //3.绘制透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSetting);
    }

    //提交缓冲区渲染命令
    void Submit(){
        buffer.EndSample(bufferName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer(){
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

}
