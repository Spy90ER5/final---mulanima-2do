Shader "HorrorEngine/URP/StandardMasked"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _MaxMaskDistance ("Max Mask Distance", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float2 screenUV : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Glossiness;
            float _Metallic;
            float _MaxMaskDistance;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);
                float distance = length(_WorldSpaceCameraPos - IN.worldPos);

                float4 albedo = tex2D(_MainTex, IN.uv) * _Color;
                float mask = tex2D(_MaskTex, IN.uv).r;

                if (distance < _MaxMaskDistance)
                    clip(mask - 0.5);

                SurfaceData surfaceData;
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = albedo.a;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = 0.0;                  // only used in specular workflow
                surfaceData.smoothness = _Glossiness;
                surfaceData.normalTS = float3(0.0, 0.0, 1.0); // assumes flat normal
                surfaceData.emission = float3(0.0, 0.0, 0.0);
                surfaceData.occlusion = 1.0;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                half3 viewDirWS = normalize(GetCameraPositionWS() - IN.worldPos);

                // Input data for lighting
               InputData inputData;
                inputData.positionWS = IN.worldPos;
                inputData.normalWS = float3(0.0, 0.0, 1.0); // flat normal
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = float4(0.0, 0.0, 0.0, 0.0);
                inputData.vertexLighting = float3(0.0, 0.0, 0.0);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = IN.positionHCS.xy / _ScreenParams.xy;

                float fogFactor = ComputeFogFactor(IN.worldPos.rgb);
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                return float4(MixFog(color, fogFactor),1);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
