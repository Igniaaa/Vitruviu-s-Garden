// Blur a 9 campioni (kernel 3x3 pesato) sulla texture della UI Image a cui viene assegnato
// come materiale — sfoca l'immagine stessa, non quello che le sta dietro. _BlurSize scala
// la distanza tra i campioni: più alto = sfocatura più larga (ma meno "morbida" a parità
// di soli 9 campioni, essendo un singolo passaggio e non un vero Gaussian multi-pass).
Shader "Custom/UIBoxBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _BlurSize ("Blur Size (px)", Range(0, 20)) = 4
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
            float4 _MainTex_TexelSize;
            float4 _Color;
            float _BlurSize;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy * _BlurSize;

                half4 col = half4(0, 0, 0, 0);
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + texel * float2(-1, -1));
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + texel * float2( 0, -1)) * 2;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + texel * float2( 1, -1));
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + texel * float2(-1,  0)) * 2;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * 4;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + texel * float2( 1,  0)) * 2;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + texel * float2(-1,  1));
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + texel * float2( 0,  1)) * 2;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + texel * float2( 1,  1));
                col /= 16.0;

                return col * IN.color;
            }
            ENDHLSL
        }
    }
}
