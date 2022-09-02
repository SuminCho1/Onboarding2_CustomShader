#ifndef CUSTOM_BLUR_INCLUDED
#define CUSTOM_BLUR_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

float4 _MainTex_TexelSize;
float4 _MainTex_ST;

int _BlurStrength;

Varyings DefaultPassVertex(Attributes input)
{
    Varyings output;
    output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    return output;
}

float4 BloomHorizontalPassFragment(Varyings input) : SV_TARGET
{
    float2 res = _MainTex_TexelSize.xy;
    float4 sum = 0;

    int samples = 2 * _BlurStrength + 1;

    for (float y = 0; y < samples; ++y)
    {
        float2 offset = float2(0, y - _BlurStrength);
        sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset * res);
    }
    
    return sum / samples;
}

float4 BloomVerticalPassFragment(Varyings input) : SV_TARGET
{
    float2 res = _MainTex_TexelSize.xy;
    float4 sum = 0;

    int samples = 2 * _BlurStrength + 1;

    for (float x = 0; x < samples; ++x)
    {
        float2 offset = float2(x - _BlurStrength, 0);
        sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset * res);
    }
    
    return sum / samples;
}

#endif
