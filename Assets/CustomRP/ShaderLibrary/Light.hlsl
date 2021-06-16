#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
//方向光数据
CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
	//定向光源颜色、方向、阴影等数据
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

//灯光属性
struct Light{
    float3 color;
    float3 direction;
};

//获取方向光源的数量
int GetDirectionalLightCount() {
	return _DirectionalLightCount;
}

//获取目标索引定向光的属性
Light GetDirectionalLight (int index) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	return light;
}

#endif