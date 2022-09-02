Shader "Custom/Aqua Color"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTexture ("Texture", 2D) = "white" {}
        _EdgeColor("Edge Color", Color) = (0, 0, 0, 1)
        _FillColor ("Fill Color", Color) = (1, 1, 1, 1)
        _Iteration ("Iteration", Float) = 20
        _Interval ("Interval", Float) = 0.73
        _BlurWidth ("Blur Width", Float) = 1
        _BlurFrequency ("Blur Frequency", Float) = 0.5
        _EdgeContrast ("EdgeContrast", Float) = 1.2
        _HueShift ("Hue Shift", Float) = 0.1
    }
    
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "Aqua"

            HLSLPROGRAM

            #pragma target 3.5
            
            #pragma vertex PassVertex
            #pragma fragment PassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/AquaFilter.hlsl"
            #include "Assets/Shaders/AquaColor.hlsl"
            
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
