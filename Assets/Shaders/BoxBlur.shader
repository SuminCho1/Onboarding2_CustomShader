Shader "Hidden/Custom/Blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white"    
    }
    
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        
        HLSLINCLUDE

        #pragma target 3.5
        #pragma vertex DefaultPassVertex
        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Assets/Shaders/Blur.hlsl"
        
        ENDHLSL
        
        Pass
        {
             Name "Bloom Horizontal"

            HLSLPROGRAM
            #pragma fragment BloomHorizontalPassFragment
            ENDHLSL
        }
        
        Pass
        {
            Name "Bloom Vertical"
            
            HLSLPROGRAM
            #pragma fragment BloomVerticalPassFragment
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
