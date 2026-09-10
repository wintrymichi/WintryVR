// WintryVR/Glass — soft translucent panel with a faint edge highlight for spatial UI (cards, menus, labels).
Shader "WintryVR/Glass"
{
    Properties
    {
        _Color ("Tint (alpha = opacity)", Color) = (0.08, 0.12, 0.2, 0.6)
        _EdgeColor ("Edge Color", Color) = (1, 1, 1, 0.35)
        _Fresnel ("Edge Power", Range(0.2, 6)) = 1.5
        _Size ("Panel size in metres (w, h)", Vector) = (1, 1, 0, 0)
        _Radius ("Corner radius in metres", Float) = 0.02
        _EdgeWidth ("Edge band width in metres", Float) = 0.006
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
                float4 _Size;
                float _Radius;
                float _EdgeWidth;
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

                // Signed distance to a rounded rectangle, evaluated in metres rather than in uv. Measuring in
                // uv made the band thicker on a panel's short axis and squared off the corners, so the glow
                // stopped following the plate's own silhouette. _Size carries the plate's real dimensions, so
                // one _EdgeWidth now means the same thickness everywhere and around the corners.
                float2 halfSize = max(_Size.xy, 1e-4) * 0.5;
                float2 p = (IN.uv - 0.5) * _Size.xy;
                float r = min(_Radius, min(halfSize.x, halfSize.y));
                float2 q = abs(p) - (halfSize - r);
                float dist = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;   // < 0 inside, 0 on the border

                half edge = saturate(1.0 + dist / max(_EdgeWidth, 1e-4));
                edge *= edge;                                    // keep the falloff tight against the rim
                half sheen = saturate(0.55h + p.y / max(_Size.y, 1e-4));

                half3 col = _Color.rgb * lerp(0.92h, 1.10h, sheen)
                          + _EdgeColor.rgb * (_EdgeColor.a * (edge * 0.75h + rim * 0.25h));
                half alpha = saturate(_Color.a + edge * 0.22h);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
