Shader "MagicTiles/Gradient"
{
    Properties
    {
        _ColorTop ("Top Color",    Color) = (1,    0.45, 0.28, 1)
        _ColorMid ("Mid Color",    Color) = (0.98, 0.32, 0.32, 1)
        _ColorBot ("Bottom Color", Color) = (0.85, 0.18, 0.22, 1)

        // SpriteRenderer writes the sprite texture here — we sample it for shape/alpha.
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        // SpriteRenderer per-instance tint (set by Color field or MPB)
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;       // per-vertex tint from SpriteRenderer
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorTop, _ColorMid, _ColorBot;
                float4 _Color;
                float4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample sprite — provides shape alpha (rounded corners, etc.)
                half4 texSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Three-stop gradient along UV.y: 0=bottom, 1=top
                float v = IN.uv.y;
                float4 gradient = v > 0.5
                    ? lerp(_ColorMid, _ColorTop, (v - 0.5) * 2.0)
                    : lerp(_ColorBot, _ColorMid, v * 2.0);

                // Apply sprite alpha so gradient is clipped to the sprite shape,
                // then apply per-instance tint
                gradient.a *= texSample.a * IN.color.a;
                gradient.rgb *= IN.color.rgb;

                return gradient;
            }
            ENDHLSL
        }
    }
}
