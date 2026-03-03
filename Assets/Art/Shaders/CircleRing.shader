Shader "Reflex/CircleRing"
{
    Properties
    {
        _Progress ("Ring Progress", Range(0, 1)) = 0
        _BaseColor ("Base Color", Color) = (0.15, 0.15, 0.2, 0.8)
        _RingColor ("Ring Color", Color) = (0, 0.9, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 0.5
        _InnerRadius ("Inner Radius", Range(0, 0.5)) = 0.15
        _OuterRadius ("Outer Radius", Range(0, 0.5)) = 0.45
        _RingThickness ("Ring Thickness", Range(0, 0.1)) = 0.025
        _EdgeSoftness ("Edge Softness", Range(0, 0.05)) = 0.008
        _GlowWidth ("Glow Width", Range(0, 0.1)) = 0.04
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
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
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Progress)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RingColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _GlowIntensity)
            UNITY_INSTANCING_BUFFER_END(Props)

            float _InnerRadius;
            float _OuterRadius;
            float _RingThickness;
            float _EdgeSoftness;
            float _GlowWidth;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float progress = UNITY_ACCESS_INSTANCED_PROP(Props, _Progress);
                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
                float4 ringColor = UNITY_ACCESS_INSTANCED_PROP(Props, _RingColor);
                float glowIntensity = UNITY_ACCESS_INSTANCED_PROP(Props, _GlowIntensity);

                // Distance from center (0 at center, ~0.707 at corner)
                float2 centeredUV = i.uv - 0.5;
                float dist = length(centeredUV);

                // Inner target circle - filled, subtle
                float innerCircle = 1.0 - smoothstep(_InnerRadius - _EdgeSoftness, _InnerRadius + _EdgeSoftness, dist);

                // Inner circle edge ring
                float innerEdge = smoothstep(_InnerRadius - _RingThickness - _EdgeSoftness, _InnerRadius - _RingThickness, dist)
                                * (1.0 - smoothstep(_InnerRadius, _InnerRadius + _EdgeSoftness, dist));

                // Closing ring position: moves from outer to inner based on progress
                float ringPosition = lerp(_OuterRadius, _InnerRadius, progress);

                // Ring band
                float ring = smoothstep(ringPosition - _RingThickness - _EdgeSoftness, ringPosition - _RingThickness, dist)
                           * (1.0 - smoothstep(ringPosition + _RingThickness, ringPosition + _RingThickness + _EdgeSoftness, dist));

                // Glow around ring
                float glowDist = abs(dist - ringPosition);
                float glow = exp(-glowDist * glowDist / (2.0 * _GlowWidth * _GlowWidth));
                glow *= glowIntensity;

                // Outer boundary circle (faint guide ring at spawn radius)
                float outerGuide = smoothstep(_OuterRadius - 0.003 - _EdgeSoftness, _OuterRadius - 0.003, dist)
                                 * (1.0 - smoothstep(_OuterRadius + 0.003, _OuterRadius + 0.003 + _EdgeSoftness, dist));
                outerGuide *= 0.2 * (1.0 - progress); // Fades as ring closes

                // Combine colors
                float4 col = float4(0, 0, 0, 0);

                // Inner filled circle
                col += innerCircle * baseColor;

                // Inner edge highlight
                col.rgb += innerEdge * ringColor.rgb * 0.6;
                col.a = max(col.a, innerEdge * 0.8);

                // Closing ring
                col.rgb += ring * ringColor.rgb;
                col.a = max(col.a, ring);

                // Glow
                col.rgb += glow * ringColor.rgb * 0.6;
                col.a = max(col.a, glow * 0.5);

                // Outer guide
                col.rgb += outerGuide * ringColor.rgb * 0.3;
                col.a = max(col.a, outerGuide * 0.3);

                // Clip fully transparent
                clip(col.a - 0.01);

                return col;
            }
            ENDCG
        }
    }

    FallBack Off
}
