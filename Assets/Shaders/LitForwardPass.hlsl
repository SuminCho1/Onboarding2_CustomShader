// #ifndef CUSTOM_LIT_PASS_INCLUDED
// #define CUSTOM_LIT_PASS_INCLUDED
//
// #include "Assets/Shaders/Surface.hlsl"
// #include "Assets/Shaders/Light.hlsl"
// #include "Assets/Shaders/Lighting.hlsl"
//
// struct Attributes
// {
//     float4 positionOS : POSITION;
//     float3 normalOS : NORMAL;
//     float2 uv : TEXCOORD0;
//
//     UNITY_VERTEX_INPUT_INSTANCE_ID
// };
//
// struct Varyings
// {
//     float4 positionCS : SV_POSITION;
//     float3 normalWS : TEXCOORD3;
//     float2 uv : TEXCOORD0;
//
//     UNITY_VERTEX_INPUT_INSTANCE_ID
// };
//
// void InitializeInputData(Varyings input, out InputData inputData)
// {
//     inputData = (InputData)0;
//
//     #if defined(DEBUG_DISPLAY)
//     inputData.positionWS = input.positionWS;
//     inputData.normalWS = input.normalWS;
//     inputData.viewDirectionWS = input.viewDirWS;
//     #else
//     inputData.positionWS = float3(0, 0, 0);
//     inputData.normalWS = half3(0, 0, 1);
//     inputData.viewDirectionWS = half3(0, 0, 1);
//     #endif
//     inputData.shadowCoord = 0;
//     inputData.fogCoord = 0;
//     inputData.vertexLighting = half3(0, 0, 0);
//     inputData.bakedGI = half3(0, 0, 0);
//     inputData.normalizedScreenSpaceUV = 0;
//     inputData.shadowMask = half4(1, 1, 1, 1);
// }
//
// Varyings LitPassVertex(Attributes input)
// {
//     Varyings output = (Varyings)0;
//
//     UNITY_SETUP_INSTANCE_ID(input);
//     UNITY_TRANSFER_INSTANCE_ID(input, output);
//     UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
//
//     VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
//
//     output.positionCS = vertexInput.positionCS;
//     output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
//     output.normalWS = TransformObjectToWorldNormal(input.normalOS);
//
//     return output;
// }
//
// half4 LitPassFragment(Varyings input) : SV_Target
// {
//     UNITY_SETUP_INSTANCE_ID(input);
//     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
//
//     const float2 uv = input.uv;
//     float4 tex_color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
//
//     const float alpha = tex_color.a * _BaseColor.a;
//     
//     Surface surface;
//     surface.normal = normalize(input.normalWS);
//     surface.color = tex_color.rgb * _BaseColor.rgb;
//     surface.alpha = alpha;
//
//     float3 color = GetLighting(surface);
//     return float4(color, surface.alpha);
// }
//
// #endif
