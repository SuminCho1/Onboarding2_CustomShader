#ifndef CUSTOM_AQUA_FILTER_INCLUDED
#define CUSTOM_AQUA_FILTER_INCLUDED

SAMPLER(s_linear_clamp_sampler);
SAMPLER(default_sampler_Linear_Repeat);

struct AquaFilter
{
    TEXTURE2D(inputTexture);
    TEXTURE2D(noiseTexture);

    float4 edgeColor;
    float4 fillColor;

    float aspectRatio;
    float aspectRatioRcp;

    uint iteration;
    float iterationRcp;

    float interval;
    float blurWidth;
    float blurFrequency;
    float edgeContrast;
    float hueShift;

    // Basic math function
    float2 Rotate90(float2 v)
    {
        return v.yx * float2(-1, 1);
    }

    // Coordinate system conversion

    // UV to vertically normalized screen coordinates
    float2 UV2SC(float2 uv)
    {
        float2 p = uv - 0.5;
        p.x *= aspectRatio;
        return p;
    }

    // Vertically normalized screen coordinates to UV
    float2 SC2UV(float2 p)
    {
        p.x *= aspectRatioRcp;
        return p + 0.5;
    }

    // Texture sampling functions

    float3 SampleColor(float2 p)
    {
        return SAMPLE_TEXTURE2D(inputTexture, s_linear_clamp_sampler, SC2UV(p)).rgb;
    }

    float3 SampleNoise(float2 p)
    {
        return SAMPLE_TEXTURE2D(noiseTexture, default_sampler_Linear_Repeat, p).rgb;
    }

    // Gradient function
    float2 GetGradient(float2 screenCoord, float freq)
    {
        const float2 dx = float2(interval / 200, 0);

        //x축의 변화량
        float ldx = SampleColor(screenCoord + dx.xy) - SampleColor(screenCoord - dx.xy);

        //y축의 변화량
        float ldy = SampleColor(screenCoord + dx.yx) - SampleColor(screenCoord - dx.yx);

        float2 noise = SampleNoise(screenCoord * 0.4 * freq).gb - 0.5;
        
        return float2(ldx, ldy) + noise * 0.05;
    }

    //외곽선 반환
    float ProcessEdge(inout float2 screenCoord, float stride)
    {
        float2 gradient = GetGradient(screenCoord, 1);
        float edge = saturate(length(gradient) * 10);
        float pattern = SampleNoise(screenCoord * 0.8).r;
        screenCoord += normalize(Rotate90(gradient)) * stride;
        return pattern * edge;
    }

    float3 HsvToRgb(real3 c)
    {
        const real4 K = real4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
        real3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
        return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
    }
    
    float3 ProcessFill(inout float2 p, float stride)
    {
        float2 gradient = GetGradient(p, blurFrequency);
        p += normalize(gradient) * stride;
        float shift = SampleNoise(p * 0.1).r * 2;
        return SampleColor(p) * HsvToRgb(float3(shift, hueShift, 1));
    }

    // Main filter function
    float3 ProcessAt(float2 uv)
    {
        // Gradient oriented blur effect

        //uv를 스크린 좌표로 변환
        float2 p = UV2SC(uv);

        float2 p_e_n = p;
        float2 p_e_p = p;
        float2 p_c_n = p;
        float2 p_c_p = p;

        const float Stride = 0.04 * iterationRcp;

        float  acc_e = 0;
        float3 acc_c = 0;
        float  sum_e = 0;
        float  sum_c = 0;

        for (uint i = 0; i < iteration; i++)
        {
            float w_e = 1.5 - i * iterationRcp;
            acc_e += ProcessEdge(p_e_n, -Stride) * w_e;
            acc_e += ProcessEdge(p_e_p, +Stride) * w_e;
            sum_e += w_e * 2;

            float w_c = 0.2 + i * iterationRcp;
            acc_c += ProcessFill(p_c_n, -Stride * blurWidth) * w_c;
            acc_c += ProcessFill(p_c_p, +Stride * blurWidth) * w_c * 0.3;
            sum_c += w_c * 1.3;
        }

        // Normalization and contrast
        acc_e /= sum_e;
        acc_c /= sum_c;

        acc_e = saturate((acc_e - 0.5) * edgeContrast + 0.5);

        // Color blending
        float3 rgb_e = lerp(1, edgeColor.rgb, edgeColor.a * acc_e);
        float3 rgb_f = lerp(1, acc_c, fillColor.a) * fillColor.rgb;

        return rgb_e * rgb_f;
    }
};

float3 LinearToSRGB(float3 c)
{
    float3 sRGBLo = c * 12.92;
    float3 sRGBHi = (PositivePow(c, float3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
    float3 sRGB = (c <= 0.0031308) ? sRGBLo : sRGBHi;
    return sRGB;
}

float3 SRGBToLinear(float3 c)
{
    #if defined(UNITY_COLORSPACE_GAMMA) && REAL_IS_HALF
    c = min(c, 100.0); // Make sure not to exceed HALF_MAX after the pow() below
    #endif
    float3 linearRGBLo = c / 12.92;
    float3 linearRGBHi = PositivePow((c + 0.055) / 1.055, float3(2.4, 2.4, 2.4));
    float3 linearRGB = (c <= 0.04045) ? linearRGBLo : linearRGBHi;
    return linearRGB;
}

#endif
