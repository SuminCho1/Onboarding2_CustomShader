#ifndef CUSTOM_POST_FX_PASSES_INCLUDED
#define CUSTOM_POST_FX_PASSES_INCLUDED

TEXTURE2D(_PostFXSource);
SAMPLER(sampler_linear_clamp);

struct Varyings {
    float4 positionCS : SV_POSITION;
    float2 screenUV : TEXCOORD0;
};

float4 _PostFXSource_TexelSize;

float4 GetSourceTexelSize()
{
    return _PostFXSource_TexelSize;
}

float4 GetSource(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource, sampler_linear_clamp, screenUV, 0);
}

Varyings DefaultPassVertex(uint vertexID : SV_VertexID)
{
    Varyings output;

    output.positionCS = float4(
        vertexID <= 1 ? -1.0f : 3.0f,
        vertexID == 1 ? 3.0f : -1.0f,
        0.0f, 1.0f);
    output.screenUV = float2(
        vertexID <= 1 ? 0.0f : 2.0f,
        vertexID == 1 ? 2.0f : 0.0f);

    if(_ProjectionParams.x < 0.0f)
        output.screenUV.y = 1.0f - output.screenUV.y;
    
    return output;
}

float4 CopyPassFragment(Varyings input) : SV_TARGET
{
    return GetSource(input.screenUV);
}

float4 BloomHorizontalPassFragment(Varyings input) : SV_TARGET
{
    
    float3 color = 0.0;
    float offsets[] = {-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0};
    float weights[] = {
        0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
        0.19459459, 0.12162162, 0.05405405, 0.01621622};

    for (int i = 0; i < 9; ++i)
    {
        float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
        color += GetSource(input.screenUV + float2(offset, 0.0)).rgb * weights[i];
    }
    return float4(color, 1.0);
}

float4 BloomVerticalPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    float offsets[] = {-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0};
    float weights[] = {
        0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
        0.19459459, 0.12162162, 0.05405405, 0.01621622};

    for (int i = 0; i < 9; ++i)
    {
        float offset = offsets[i] * GetSourceTexelSize().y;
        color += GetSource(input.screenUV + float2(0.0, offset)).rgb * weights[i];
    }
    return float4(color, 1.0);
}

#endif