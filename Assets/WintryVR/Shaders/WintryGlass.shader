// WintryVR/Glass — soft translucent panel with a faint edge highlight for spatial UI (cards, menus, labels).
Shader "WintryVR/Glass"
{
    Properties
    {
        _Color ("Tint (alpha = opacity)", Color) = (0.08, 0.12, 0.2, 0.6)
        _EdgeColor ("Edge Color", Color) = (1, 1, 1, 0.35)
        _Fresnel ("Edge Power", Range(0.2, 6)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Glass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EdgeColor;
                half _Fresnel;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
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
                OUT.uv = IN.uv;
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
                half ndv = abs(dot(n, v));
                half rim = pow(1.0h - ndv, _Fresnel);
                // subtle vignette toward the panel edges (uv based, works for rounded rects)
                half2 d = abs(IN.uv - 0.5h) * 2.0h;
                half edge = saturate((max(d.x, d.y) - 0.86h) / 0.14h);
                half3 col = _Color.rgb + _EdgeColor.rgb * (_EdgeColor.a * (edge * 0.6h + rim * 0.4h));
                half alpha = saturate(_Color.a + edge * 0.25h);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
