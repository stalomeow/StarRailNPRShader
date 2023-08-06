Shader "Hidden/Honkai Star Rail/Post Processing/UberPost"
{
    Properties
    {
        _BloomIntensity("Bloom Intensity", Float) = 0
        _BloomTint("Bloom Tint", Color) = (1, 1, 1, 1)
        _BloomTexture("Bloom Texture", 2D) = "black" {}

        _ACESParamA("ACES Param A", Float) = 2.80
        _ACESParamB("ACES Param B", Float) = 0.40
        _ACESParamC("ACES Param C", Float) = 2.10
        _ACESParamD("ACES Param D", Float) = 0.50
        _ACESParamE("ACES Param E", Float) = 1.50
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_local_fragment _ _BLOOM
            #pragma multi_compile_local_fragment _ _TONEMAPPING_ACES

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _BloomIntensity;
                float4 _BloomTint;

                float _ACESParamA;
                float _ACESParamB;
                float _ACESParamC;
                float _ACESParamD;
                float _ACESParamE;
            CBUFFER_END

            #if defined(_BLOOM)
                TEXTURE2D(_BloomTexture);
            #endif

            float3 CustomACESTonemapping(float3 x)
            {
                float3 u = _ACESParamA * x + _ACESParamB;
                float3 v = _ACESParamC * x + _ACESParamD;
                return saturate((x * u) / (x * v + _ACESParamE));
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float4 color = FragBlit(i, sampler_PointClamp);

                #if defined(_BLOOM)
                    float4 bloom = SAMPLE_TEXTURE2D(_BloomTexture, sampler_LinearClamp, i.texcoord);
                    color.rgb += bloom.rgb * _BloomTint.rgb * _BloomIntensity;
                #endif

                #if defined(_TONEMAPPING_ACES)
                    color.rgb = CustomACESTonemapping(color.rgb);
                #endif

                return color;
            }
            ENDHLSL
        }
    }
}
