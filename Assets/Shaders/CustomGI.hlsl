#ifndef CUSTOM_GI_INCLUDED
#define CUSTOM_GI_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#if defined(LIGHTMAP_ON)
    #define GI_ATTRIBUTE_DATA float2 lightMapUv : TEXCOORD1;
    #define GI_VARYINGS_DATA float2 lightMapUv : VAR_LIGHT_MAP_UV;
    #define TRANSFER_GI_DATA(input, output) \
        output.lightMapUv = input.lightMapUv * \
        unity_LightmapST.xy + unity_LightmapST.zw;
    #define GI_FRAGMENT_DATA(input) input.lightMapUv
#else
    #define GI_ATTRIBUTE_DATA
    #define GI_VARYINGS_DATA
    #define TRANSFER_GI_DATA(input, output)
    #define GI_FRAGMENT_DATA(input) 0.0
#endif

struct CustomGI
{
    float3 diffuse;
};

float3 SampleLightMap(float2 lightMapUv)
{
#if defined(LIGHTMAP_ON)
    return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),
        lightMapUv, float4(1, 1, 0, 0),
#if defined(UNITY_LIGHTMAP_FULL_HDR)
        false,
#else
        true,
#endif
        float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0f, 0.0f)
        );
#else
    return 0.0f;
#endif
}

float3 SampleLightProbe(Surface surfaceWS)
{
#if defined(LIGHTMAP_ON)
    return 0.0f;
#else
    float4 coefficients[7];
    coefficients[0] = unity_SHAr;
    coefficients[1] = unity_SHAg;
    coefficients[2] = unity_SHAb;
    coefficients[3] = unity_SHBr;
    coefficients[4] = unity_SHBg;
    coefficients[5] = unity_SHBb;
    coefficients[6] = unity_SHC;
    return max(0.0f, SampleSH9(coefficients, surfaceWS.normal));
#endif
}

CustomGI GetGI(float2 lightMapUv, Surface surfaceWS)
{
    CustomGI globalIllumination;
    globalIllumination.diffuse = SampleLightMap(lightMapUv) + SampleLightProbe(surfaceWS);
    return globalIllumination;
}

#endif
