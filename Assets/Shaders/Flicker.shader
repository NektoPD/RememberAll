Shader "TextMeshPro/Distance Field (StaticNoise)"
{
    Properties
    {
        [HDR]_FaceColor("Face Color", Color) = (1,1,1,1)
        _MainTex("Font Atlas (SDF)", 2D) = "white" {}

        _Threshold("SDF Threshold", Range(0.2,0.9)) = 0.5
        _Softness("SDF Softness", Range(0.0,0.2))   = 0.08

        _NoiseScale("Noise Scale", Range(1,64)) = 18
        _NoiseStrength("Noise Strength", Range(0,1)) = 0.55
        _Flicker("Flicker Strength", Range(0,1)) = 0.25
        _Speed("Speed", Range(0,5)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _FaceColor;
            float _Threshold, _Softness;
            float _NoiseScale, _NoiseStrength, _Flicker, _Speed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 col : COLOR;
                float2 nUV : TEXCOORD1;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = v.color * _FaceColor;
                o.nUV = v.uv * _NoiseScale;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float sdf = tex2D(_MainTex, i.uv).a;
                float a = smoothstep(_Threshold - _Softness, _Threshold + _Softness, sdf);
                if (a <= 0.001) discard;

                // клеточный (value) noise на фрагменте
                float2 cell = floor(i.nUV);
                float n = hash21(cell + floor(_Time.y * _Speed * 60.0)); // «перещёлкивается» во времени
                float flicker = 0.5 + 0.5 * sin(_Time.y * _Speed * 6.2831);

                float noiseMul = (1.0 - _NoiseStrength * n) * (1.0 - _Flicker * (flicker * 0.5 + 0.5));

                float finalA = saturate(a * noiseMul);

                float3 face = i.col.rgb * (0.9 + 0.1 * n); // лёгкая вариация яркости
                return float4(face, finalA * i.col.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}
