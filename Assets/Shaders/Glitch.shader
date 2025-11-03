Shader "TextMeshPro/Distance Field (GlitchNoiseMesh)"
{
    Properties
    {
        [HDR]_FaceColor("Face Color", Color) = (1,1,1,1)
        _MainTex("Font Atlas (SDF)", 2D) = "white" {}
        _NoiseTex("Noise (Tileable)", 2D) = "gray" {}

        // SDF edge
        _Threshold("SDF Threshold", Range(0.2,0.9)) = 0.5
        _Softness("SDF Softness", Range(0.0,0.2)) = 0.08

        // Fragment noise
        _NoiseAmount("Noise Strength", Range(0,1)) = 0.6
        _GlitchAmount("UV Glitch Shift", Range(0,0.02)) = 0.008
        _ScanlineDensity("Scanline Density", Range(16,512)) = 160
        _Speed("Anim Speed", Range(0,5)) = 1.2

        // >>> NEW: vertex glitch <<<
        _VertGlitchAmp("Vertex Glitch Amplitude", Range(0,5)) = 0.7
        _VertBands("Vertex Bands (per unit Y)", Range(1,200)) = 60
        _VertJitterAmp("Random Jitter Amp", Range(0,3)) = 0.35
        _VertTimeScale("Vertex Time Scale", Range(0,6)) = 2.0

        // Safety: infinitesimal pad to reduce clipping (visual only)
        _PixelPad("Visual Pixel Pad (not layout)", Range(0,2)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"
        }
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
            sampler2D _NoiseTex;
            float4 _FaceColor;
            float _Threshold, _Softness;
            float _NoiseAmount, _GlitchAmount, _ScanlineDensity, _Speed;

            // vertex glitch
            float _VertGlitchAmp, _VertBands, _VertJitterAmp, _VertTimeScale;
            float _PixelPad;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0; // SDF sampling — неизменные UV
                float4 col : COLOR;
                float2 uvN : TEXCOORD1; // noise UV
                float2 screenUV : TEXCOORD2;
            };

            // simple hash
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            // псевдослучайный 2D → 2D
            float2 hash22(float2 p)
            {
                float n = hash21(p);
                return frac(float2(n, n * 1.2153) + float2(0.123, 0.517));
            }

            v2f vert(appdata v)
            {
                v2f o;

                // Сохраняем исходные UV для SDF — контур букв не «плывёт»
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = v.color * _FaceColor;

                // ---------- Vertex Glitch ----------
                // Локальные координаты меша (до проекции)
                float2 lp = v.vertex.xy;

                // Полосовой «сдвиг» по Y (сканлайны), не влияющий на UV
                float t = _Time.y * _VertTimeScale;
                // частота полос — в «полосах на юнит»
                float bandPhase = floor(lp.y * _VertBands);
                // случайный вектор для полосы
                float2 bandRand = hash22(float2(bandPhase, 0.0));
                // синус для динамики
                float bandSine = sin(t * (1.5 + bandRand.x * 3.0) + bandPhase * 0.37);

                // амплитуда сдвига для полосы
                float2 bandShift = (bandRand - 0.5) * 2.0 * _VertGlitchAmp * bandSine;

                // Пер-вершинный случайный джиттер (мелкая дрожь)
                float2 jitterRand = hash22(lp * 17.3 + t);
                float2 jitter = (jitterRand - 0.5) * 2.0 * _VertJitterAmp;

                // Суммарное смещение вершин (в локальных координатах x/y)
                float2 totalShift = bandShift + jitter;

                // Микроскопический «пэддинг» (визуальный) — расширяет букву,
                // но не меняет макет. Помогает от «съедания» краёв при масках.
                float2 pixelPad = normalize(totalShift + 1e-5) * _PixelPad;

                // Применяем смещение
                float4 vtx = v.vertex;
                vtx.xy += totalShift + pixelPad;

                // -----------------------------------

                o.pos = UnityObjectToClipPos(vtx);

                // шумовые UV
                o.uvN = v.uv * float2(6.0, 6.0);

                // нормализованный экранный UV для сканлайнов (фрагмент)
                float4 sp = o.pos / o.pos.w;
                o.screenUV = sp.xy * 0.5 + 0.5;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Берём альфу SDF
                float sdf = tex2D(_MainTex, i.uv).a;

                // Градиент края: чем выше Softness, тем мягче контур
                float edge = smoothstep(_Threshold - _Softness, _Threshold + _Softness, sdf);

                // Отбрасываем пиксели, где буквы нет
                if (edge <= 0.001)
                    discard;

                // Сканлайны
                float scan = 0.5 + 0.5 * sin(i.screenUV.y * _ScanlineDensity + _Time.y * _Speed * 3.14159);

                // Лёгкий UV-глитч (только для видимых пикселей)
                float band = step(0.85, frac(i.screenUV.y * 7.0 + _Time.y * _Speed * 0.37));
                float2 glitchUV = i.uv + float2(band * (_GlitchAmount * (hash21(i.uvN + _Time.y) * 2 - 1)), 0);

                // Шум
                float2 nUV = i.uvN + float2(_Time.y * 0.11, _Time.y * 0.07);
                float noiseTex = tex2D(_NoiseTex, nUV).r;
                float noiseHash = hash21(i.uv * 512 + _Time.y);
                float noise = lerp(noiseTex, noiseHash, 0.35);

                // Модуляция яркости и альфы
                float noisyEdge = edge * (1.0 - _NoiseAmount * (0.35 + 0.65 * noise)) * (0.85 + 0.15 * scan);

                // Эффект «обрыва» символа
                float cutBand = step(0.9, frac(i.screenUV.y * 5.0 + _Time.y * _Speed * 0.29));
                float cut = lerp(1.0, step(0.4, frac(glitchUV.x * 30.0 + _Time.y)), cutBand * 0.25);

                float finalA = saturate(noisyEdge * cut);

                // Цвет символа
                float3 face = i.col.rgb;
                face *= lerp(1.0, 0.92, _NoiseAmount);

                // ✅ итог — только буквы, без фона!
                return float4(face, finalA * i.col.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}