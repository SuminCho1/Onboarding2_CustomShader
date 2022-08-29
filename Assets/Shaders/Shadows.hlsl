#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct DirectionalShadowData
{
    float strength;
    int tileIndex;
};

float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    //그림자 텍스쳐에서 특정좌표의 값 구함
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float GetDirectionalShadowAttenuation(DirectionalShadowData data, Surface surfaceWS)
{
    if(data.strength <= 0.0f)
        return 1.0f;
    
    //그림자가 월드 좌표로 변환됨
    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex],
        float4(surfaceWS.position, 1.0f)).xyz;

    float shadow = SampleDirectionalShadowAtlas(positionSTS);
    return lerp(1.0f, shadow, data.strength);
}

#endif