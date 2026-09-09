// WintryVR/Glow — mobile-friendly URP unlit glow with fresnel rim, used by the Core orb, eyes, halo,
// highlights and pointers. Single pass, SRP Batcher compatible, single-pass instanced stereo aware.
Shader "WintryVR/Glow"
{
    Properties
    {
        _Color ("Color", Color) = (0.6, 0.85, 1, 1)
        _EmissionColor ("Emission", Color) = (0.45, 0.8, 1, 1)
        _EmissionStrength ("Emission Strength", Range(0, 4)) = 1.5
        _Fresnel ("Fresnel Power", Range(0.2, 6)) = 2
        _Alpha ("Alpha", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "GlowUnlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EmissionColor;
                half _EmissionStrength;
                half _Fresnel;
                half _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = _WorldSpaceCameraPos.xyz - positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                half3 n = normalize(IN.normalWS);
                half3 v = normalize(IN.viewDirWS);
                half ndv = saturate(dot(n, v));
                half rim = pow(1.0h - ndv, _Fresnel);
                half3 col = _Color.rgb + _EmissionColor.rgb * _EmissionStrength * (0.35h + 0.65h * rim);
                half alpha = saturate(_Alpha * (0.55h + 0.45h * rim));
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
