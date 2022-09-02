Shader "Hidden/Custom/Post FX Stack"
{
    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off
        
        HLSLINCLUDE
        #include "Assets/Shaders/Common.hlsl"
        #include "Assets/Shaders/PostFXStackPasses.hlsl"
        ENDHLSL
        
        Pass
        {
            //이름을 사용할 경우, 같은 쉐이더에서 패스들을 결합하기 쉽게 함
            Name "Copy"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CopyPassFragment
            ENDHLSL
        }
        
        Pass
        {
            Name "Bloom Horizontal"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomHorizontalPassFragment
            ENDHLSL
        }
        
        Pass
        {
            Name "Bloom Vertical"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomVerticalPassFragment
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "CustomShaderGUI"
}
