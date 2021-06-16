using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    const string bufferName = "Lighting";

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

    //设置最大可见定向光数量
    const int maxDirLightCount = 4;
    static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColors");
    static int dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirections");
    //存储定向光的颜色和方向
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];

    CullingResults cullingResults;
    Shadows shadows = new Shadows();

    public void SetUp(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings){
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        shadows.SetUp(context, cullingResults, shadowSettings);
        //发送光源数据
        // SetupDirectionLight();
        SetupLights();
        shadows.Render();

        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupDirectionalLight(int index, ref VisibleLight visibleLight){
        dirLightColors[index] = visibleLight.finalColor;
        //通过VisibleLight.localToWorldMatrix属性找到前向矢量,它在矩阵第三列，还要进行取反
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }

    void SetupLights(){
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];

            if (visibleLight.lightType == LightType.Directional)
            {
                //VisibleLight结构很大,我们改为传递引用不是传递值，这样不会生成副本
                SetupDirectionalLight(dirLightCount++,ref visibleLight);
                //当超过灯光限制数量中止循环
                if (dirLightCount >= maxDirLightCount)
                {
                    break;
                }
            }
        }

        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        buffer.SetGlobalVectorArray(dirLightColorId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionId, dirLightDirections);
    }

    public void Cleanup(){
        shadows.Clearup();
    }

}
