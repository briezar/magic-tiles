Shader "MagicTiles/GradientNoise"
{
    Properties
    {
        _ColorTop    ("Top Color",    Color) = (1, 0.45, 0.28, 1)
        _ColorMid    ("Mid Color",    Color) = (0.98, 0.32, 0.32, 1)
        _ColorBot    ("Bottom Color", Color) = (0.85, 0.18, 0.22, 1)
        _NoiseScale  ("Noise Scale",  Float) = 4.0
        _NoiseSpeed  ("Noise Speed",  Float) = 0.12
        _NoiseAmt    ("Noise Amount", Range(0, 0.12)) = 0.04
        _PulseAmt    ("Music Pulse",  Range(0, 1))    = 0.0

        // The shader does not use this, but the property must exist for SpriteRenderer.
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorTop, _ColorMid, _ColorBot;
                float  _NoiseScale, _NoiseSpeed, _NoiseAmt, _PulseAmt;
                TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);  // declared but never sampled
                float4 _MainTex_ST;
            CBUFFER_END

            // Simple value noise
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1,0)), f.x),
                            lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), f.x), f.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float v = IN.uv.y;
                // Three-stop gradient
                float4 col = v > 0.5
                    ? lerp(_ColorMid, _ColorTop, (v - 0.5) * 2.0)
                    : lerp(_ColorBot, _ColorMid, v * 2.0);

                // Subtle noise shimmer
                float n = noise(IN.uv * _NoiseScale + float2(0, _Time.y * _NoiseSpeed));
                col.rgb += (n - 0.5) * _NoiseAmt;

                // Music pulse brightens top
                col.rgb += _PulseAmt * 0.12 * float3(0.8, 0.9, 1.0) * v;

                return col;
            }
            ENDHLSL
        }
    }
}
