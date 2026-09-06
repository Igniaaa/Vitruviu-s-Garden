// Sfuma l'alpha del pannello verso i bordi del suo rettangolo (in spazio UV), con un
// rumore che rende il confine irregolare invece di un fade perfettamente uniforme, per
// un effetto "bordo fumoso" invece del classico taglio netto rettangolare. Funziona anche
// su una texture a tinta piatta (es. pannello nero), perché non dipende dal contenuto della
// texture ma solo dalla distanza dal bordo in UV.
Shader "Custom/UISmokyEdge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0, 0, 0, 0.85)
        _FeatherSize ("Feather Size", Range(0.001, 0.5)) = 0.15
        _NoiseAmount ("Smoke Irregularity", Range(0, 1)) = 0.3
        _NoiseScale ("Smoke Scale", Range(1, 50)) = 10
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _Color;
            float _FeatherSize;
            float _NoiseAmount;
            float _NoiseScale;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // distanza dal bordo più vicino in UV: 0 sul bordo, 0.5 al centro
                float2 distToEdge = min(IN.uv, 1 - IN.uv);
                float edgeDist = min(distToEdge.x, distToEdge.y);

                float smoke = (Noise(IN.uv * _NoiseScale) - 0.5) * _NoiseAmount;
                float feather = smoothstep(0, _FeatherSize, edgeDist + smoke);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                col.a *= feather;

                return col;
            }
            ENDHLSL
        }
    }
}
