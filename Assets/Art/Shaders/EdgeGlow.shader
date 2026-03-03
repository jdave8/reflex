Shader "Reflex/EdgeGlow"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (0, 0.9, 1, 0.6)
        _Intensity ("Intensity", Range(0, 2)) = 0
        _Falloff  ("Falloff",   Range(0.5, 5)) = 2.0
        _EdgeWidth("Edge Width", Range(0.1, 0.8)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _GlowColor;
            float  _Intensity;
            float  _Falloff;
            float  _EdgeWidth;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Remap UV so (0,0) = center, edges = ±1
                float2 centeredUV = i.uv * 2.0 - 1.0;
                float dist = length(centeredUV);

                // Inverse vignette: glow at edges, transparent at center
                float edge = smoothstep(1.0 - _EdgeWidth, 1.0 + 0.1, dist);
                edge = pow(edge, _Falloff);

                float alpha = edge * _Intensity * _GlowColor.a;
                return float4(_GlowColor.rgb, saturate(alpha));
            }
            ENDCG
        }
    }

    FallBack Off
}
