using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    bool useDynamicBatching,useGPUInstancing;
    ShadowSettings shadowSettings;

    public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadowSettings){
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
    }

    CameraRenderer renderer = new CameraRenderer();
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //启用SRP合批 set pass call
        GraphicsSettings.useScriptableRenderPipelineBatching = true;

        for (int i = 0; i < cameras.Length; i++)
        {
            renderer.Render(context, cameras[i], useDynamicBatching, useGPUInstancing, shadowSettings);
        }
    }
}
