#ifndef CUSTOM_AQUA_COLOR_INCLUDED
#define CUSTOM_AQUA_COLOR_INCLUDED

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
TEXTURE2D(_NoiseTexture);

float4 _MainTex_ST;

float4 _EdgeColor;
float4 _FillColor;
float _Iteration;
float _Interval;
float _BlurWidth;
float _BlurFrequency;
float _EdgeContrast; 
float _HueShift;

Varyings PassVertex(Attributes input)
{
    Varyings output;
    output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    return output;
}

float4 PassFragment(Varyings input) : SV_TARGET
{
    AquaFilter aqua;
    aqua.inputTexture = _MainTex;
    aqua.noiseTexture = _NoiseTexture;
    
    aqua.edgeColor = _EdgeColor;
    aqua.fillColor = _FillColor;
    
    uint width, height;
    _MainTex.GetDimensions(width, height);
    
    aqua.aspectRatio = (float)width / height;
    aqua.aspectRatioRcp = 1 / aqua.aspectRatio;
    
    aqua.iteration = _Iteration;
    aqua.iterationRcp = 1.0 / _Iteration;
    
    aqua.interval      = _Interval;
    aqua.blurWidth     = _BlurWidth;
    aqua.blurFrequency = _BlurFrequency;
    aqua.edgeContrast  = _EdgeContrast;
    aqua.hueShift      = _HueShift;
    
    float3 output = aqua.ProcessAt(input.uv);
    output = LinearToSRGB(output);
    output = SRGBToLinear(output);
    
    return float4(output, 1.0);
}

#endif
