// WintryVR/Glass — liquid glass for spatial UI: a slab with a real bevelled edge that bends what is behind it,
// catches a specular streak along the rim and splits light into colour where it is thinnest.
//
// The panel is flat geometry, so all of its apparent thickness is reconstructed in the fragment shader from a
// signed distance to its own outline. That distance gives three things at once: where the bevel is, which way it
// faces, and how steep it is. Everything else follows from those.
//
// Passthrough note: on Quest the real room is composited underneath by the runtime and is not in the colour
// buffer, so _CameraOpaqueTexture holds virtual content only. Refraction therefore bends other panels and Wintry
// behind the glass, not the room; the room shows through by ordinary alpha, which is what you want in MR anyway.
// _Refraction stays 0 unless WintryMaterials finds that the pipeline actually writes an opaque texture.
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

        _Corner ("Corner continuity (2 = circular, 4+ = squircle)", Range(2, 8)) = 4
        _Bevel ("Bevel width in metres", Float) = 0.012
        _Refraction ("Refraction (0 = off)", Range(0, 0.08)) = 0
        _Dispersion ("Chromatic dispersion", Range(0, 1)) = 0.35
        _Specular ("Specular strength", Range(0, 4)) = 0.7
        _Gloss ("Specular tightness", Range(8, 256)) = 140
        _LightDir ("Key light direction", Vector) = (-0.42, 0.76, -0.5, 0)
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EdgeColor;
                half _Fresnel;
                float4 _Size;
                float _Radius;
                float _EdgeWidth;
                float _Corner;
                float _Bevel;
                float _Refraction;
                half _Dispersion;
                half _Specular;
                half _Gloss;
                float4 _LightDir;
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
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
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
                // the plate lies in its own XY, so its object axes are the surface tangent frame
                OUT.tangentWS = TransformObjectToWorldDir(float3(1, 0, 0));
                OUT.bitangentWS = TransformObjectToWorldDir(float3(0, 1, 0));
                OUT.viewDirWS = _WorldSpaceCameraPos.xyz - positionWS;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            // Signed distance to a rounded rectangle whose corners follow a p-norm rather than a circle.
            // At _Corner = 2 this is the ordinary circular round-rect; higher exponents stretch the corner into
            // a squircle, where curvature ramps in gradually instead of starting abruptly at the tangent point.
            // That continuity is most of why a shape reads as designed rather than as a rectangle with the ends
            // filed off, and it is the corner Apple's hardware and interfaces use.
            float SdSquircle(float2 p, float2 halfSize, float r, float n, out float2 outward)
            {
                float2 q = abs(p) - (halfSize - r);
                float2 m = max(q, 0.0);
                float norm = pow(pow(m.x, n) + pow(m.y, n), 1.0 / n);
                if (norm > 1e-5)
                {
                    // gradient of the p-norm, which points straight out of the outline
                    outward = normalize(sign(p) * pow(max(m / norm, 1e-5), n - 1.0));
                }
                else
                {
                    outward = (q.x > q.y) ? float2(sign(p.x), 0.0) : float2(0.0, sign(p.y));
                }
                return norm + min(max(q.x, q.y), 0.0) - r;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 halfSize = max(_Size.xy, 1e-4) * 0.5;
                float2 p = (IN.uv - 0.5) * _Size.xy;
                float r = min(_Radius, min(halfSize.x, halfSize.y));

                float2 outward;
                float dist = SdSquircle(p, halfSize, r, _Corner, outward);

                // How far into the bevel we are: 0 across the flat middle, 1 right at the outline.
                float bevel = max(_Bevel, 1e-4);
                float bevelT = saturate(1.0 + dist / bevel);
                // ease it so the slab stays flat in the middle and turns over quickly near the rim, the way a
                // polished edge does — a linear ramp reads as a chamfer instead
                float slope = bevelT * bevelT * (3.0 - 2.0 * bevelT);

                // Rebuild the surface normal the bevel would have had, in the plate's own tangent frame.
                float3 n = normalize(IN.normalWS);
                float3 t = normalize(IN.tangentWS);
                float3 b = normalize(IN.bitangentWS);
                float3 bent = normalize(n + (t * outward.x + b * outward.y) * slope * 1.7);
                float3 v = normalize(IN.viewDirWS);

                // --- refraction: bend what is behind the glass, strongest where the bevel is steepest
                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 1e-5);
                float2 push = outward * slope * _Refraction;
                half3 behind = 0;
                if (_Refraction > 1e-5)
                {
                    // sampling the channels at slightly different offsets splits the edge into colour, which is
                    // what thick glass actually does and what stops a panel reading as flat tinted plastic
                    float2 disp = push * _Dispersion * 0.5;
                    behind.r = SampleSceneColor(screenUV - push - disp).r;
                    behind.g = SampleSceneColor(screenUV - push).g;
                    behind.b = SampleSceneColor(screenUV - push + disp).b;
                }

                // --- body: tint over whatever came through
                half3 col = lerp(behind, _Color.rgb, _Refraction > 1e-5 ? _Color.a : 1.0);

                // vertical sheen, brighter along the top edge where a slab catches the room
                half sheen = saturate(0.55h + p.y / max(_Size.y, 1e-4));
                col *= lerp(0.9h, 1.12h, sheen);

                // --- specular streak along the bevel
                float3 l = normalize(_LightDir.xyz);
                float3 h = normalize(l + v);
                half spec = pow(saturate(dot(bent, h)), _Gloss) * _Specular;
                // only the bevel is curved enough to catch it; the flat face would just mirror the light once
                spec *= slope * 0.9h + 0.1h;

                // --- rim: a bright hairline exactly on the outline, plus a soft inner caustic just inside it
                half hairline = saturate(1.0 + dist / max(_EdgeWidth, 1e-4));
                hairline *= hairline;
                half caustic = smoothstep(0.35h, 1.0h, slope) * (1.0h - hairline) * 0.5h;

                half rim = pow(1.0h - saturate(dot(bent, v)), _Fresnel);
                col += _EdgeColor.rgb * _EdgeColor.a * (hairline * 0.85h + caustic + rim * 0.25h);
                col += spec;

                // Opacity rises through the bevel: a slab is thicker at the edge, and it hides its own outline
                // less. Anti-aliasing the last fraction of a millimetre keeps the squircle smooth without MSAA.
                half aa = saturate(0.5h - dist / fwidth(dist));
                half alpha = saturate(_Color.a + hairline * 0.3h + slope * 0.12h);
                // A refracting plate already carries the background, so it needs a little more body than a
                // purely translucent one — but only a little: over passthrough the room is the backdrop and
                // burying it defeats the point of doing this in MR at all.
                if (_Refraction > 1e-5) alpha = saturate(alpha + 0.12h);
                return half4(col, alpha * aa);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
