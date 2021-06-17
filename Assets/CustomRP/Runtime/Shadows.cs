using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    const string bufferName = "Shadows";

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

    //可投射阴影的平行光数量
    const int maxShadowedDirectionalLightCount = 1;
    struct ShadowedDirectionalLight{
        public int visibleLightIndex;
    }

    //存储可投射阴影的定向光源的数据
	ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    //已存储的可投射阴影的定向光数量
	int ShadowedDirectionalLightCount;

    CullingResults cullingResults;
    ScriptableRenderContext context;
    ShadowSettings settings;

    public void SetUp(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings){
        this.cullingResults = cullingResults;
        this.context = context;
        this.settings = shadowSettings;
        ShadowedDirectionalLightCount = 0;
    }

    void ExecuteBuffer(){
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    /// <summary>
    /// 存储定向光源的阴影数据
    /// </summary>
    /// <param name="light"></param>
    /// <param name="visibleLightIndex"></param>
    /// <returns></returns>
	public void ReserveDirectionalShadows(Light light, int visibleLightIndex) {
		if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount 
            && light.shadows != LightShadows.None && light.shadowStrength > 0f
			&& cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b))
		{
			ShadowedDirectionalLights[ShadowedDirectionalLightCount++] = new ShadowedDirectionalLight{ visibleLightIndex = visibleLightIndex };
        }
    }

    //阴影渲染
    public void Render(){
        if(ShadowedDirectionalLightCount > 0){
            RenderDirectionalShadows();
        }
    }

    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    //渲染平行光阴影
    void RenderDirectionalShadows(){
        //
        int atlasSize = (int)settings.directional.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        //指定渲染数据存储到RT中
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //清除深度缓冲区
        buffer.ClearRenderTarget(true, false, Color.clear);
        
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        //遍历所有方向光渲染阴影
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, atlasSize);
        }

        buffer.EndSample(bufferName);
    }

    void RenderDirectionalShadows(int index, int tileSize){
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, 0, 1, Vector3.zero, tileSize,
            0f, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

        shadowSettings.splitData = splitData;
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    public void Clearup(){
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

}
