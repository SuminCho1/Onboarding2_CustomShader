Shader "Custom/Lit"
{
    Properties
    {
//        [HideInInspector] _SampleGI("SampleGI", float) = 0.0 // needed from bakedlit
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _Cutoff("AlphaCutout", Range(0.0, 1.0)) = 0.5
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows("Shadows", Float) = 0
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0

        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) =0.5
        
        [NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}
        [HDR] _EmissionColor("Emission", Color) = (0.0, 0.0, 0.0, 0.0)
        
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
    }

    SubShader
    {
        HLSLINCLUDE
        #include "Assets/Shaders/Common.hlsl"
        #include "Assets/Shaders/LitInput.hlsl"
        ENDHLSL
        
        Pass
        {
            Tags
            {
                "LightMode" = "CustomLit"
            }
            
            Name "Lit"

            Blend [_SrcBlend][_DstBlend]
            ZWrite [_ZWrite]
            
            HLSLPROGRAM
            #pragma target 3.5
            
            // // -------------------------------------
            // // Unity defined keywords
            // #pragma multi_compile _ DOTS_INSTANCING_ON
            // #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            // #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            // #pragma multi_compile _ DEBUG_DISPLAY

            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_instancing
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Assets/Shaders/CustomLitForwardPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"    
            }
            
            ColorMask 0
            
            HLSLPROGRAM

            #pragma target 3.5
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma multi_compile_instancing
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #include "Assets/Shaders/ShadowCasterPass.hlsl"
            
            ENDHLSL
        }
        
        Pass
        {
            Tags
            {
                "LightMode" = "Meta"
            }
            
            Cull Off
            
            HLSLPROGRAM
            
            #pragma target 3.5
            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment
            #include "Assets/Shaders/MetaPass.hlsl"
            
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "CustomShaderGUI"
}
