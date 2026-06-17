// Option A: Custom Shader
// Place in any Resources or Shaders folder.
// Assign to a Material and set on _bodyRenderer.material.
// Gradient runs from _ColorBottom (UV.y = 0, tile foot) to _ColorTop (UV.y = 1, tile head).
// Works with SpriteRenderer.size — UVs always span [0,1] regardless of size.y value.

Shader "MagicTiles/HoldNoteGradient"
{
    Properties
    {
        _ColorTop    ("Color Top",    Color) = (0.3,  0.7, 1.0, 1)
        _ColorBottom ("Color Bottom", Color) = (0.05, 0.2, 0.5, 1)

        // Keep standard sprite properties so the material works in the SpriteRenderer pipeline
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "PreviewType"     = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            fixed4 _ColorBottom;
            fixed4 _ColorTop;

            struct v2f_grad
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float  uvY      : TEXCOORD1; // raw sprite UV.y for gradient
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f_grad vert(appdata_t IN)
            {
                v2f_grad OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex   = UnityFlipSprite(IN.vertex, _Flip);
                OUT.vertex   = UnityObjectToClipPos(OUT.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.uvY      = IN.texcoord.y;  // [0 = bottom, 1 = top]
                OUT.color    = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(v2f_grad IN) : SV_Target
            {
                fixed4 texColor = SampleSpriteTexture(IN.texcoord) * IN.color;

                // Blend gradient — uvY=0 is foot, uvY=1 is head
                fixed4 gradient = lerp(_ColorBottom, _ColorTop, IN.uvY);

                // Preserve alpha from the sprite, apply gradient as RGB
                texColor.rgb = gradient.rgb * texColor.a;
                texColor.a   = gradient.a   * texColor.a;

                return texColor;
            }
            ENDCG
        }
    }
}
