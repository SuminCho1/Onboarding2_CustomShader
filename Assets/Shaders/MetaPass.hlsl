#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "Assets/Shaders/Surface.hlsl"
#include "Assets/Shaders/Shadows.hlsl"
#include "Assets/Shaders/CustomLight.hlsl"
#include "Assets/Shaders/BRDF.hlsl"

struct Attributes {
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float2 lightMapUV : TEXCOORD1;
};

struct Varyings {
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
};

bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

Varyings MetaPassVertex (Attributes input) {
    Varyings output;

    input.positionOS.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    input.positionOS.z = input.positionOS.z > 0.0f ? FLT_MIN : 0.0f;
    output.positionCS = TransformWorldToHClip(input.positionOS);
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 MetaPassFragment (Varyings input) : SV_TARGET {
    float4 base = GetBase(input.baseUV);
    
    Surface surface;
    ZERO_INITIALIZE(Surface, surface);
    surface.color = base.rgb;
    surface.metallic = GetMetallic(input.baseUV);
    surface.smoothness = GetSmoothness(input.baseUV);
    
    BRDF brdf = GetBRDF(surface);

    float4 meta = 0.0;
    if (unity_MetaFragmentControl.x)
    {
        meta = float4(brdf.diffuse, 1.0f);
        meta.rgb += brdf.specular * brdf.roughness * 0.5f;
        meta.rgb =
            min(PositivePow(meta.rgb, unity_OneOverOutputBoost),
                unity_MaxOutputValue);
    }
    else if (unity_MetaFragmentControl.y)
    {
        meta = float4(GetEmission(input.baseUV), 1.0f);
    }
    
    return meta;
}

#endif