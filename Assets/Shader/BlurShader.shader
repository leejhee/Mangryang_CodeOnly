Shader "Custom/GaussianBlur"
{
    Properties
    {
        _BlurRadius ("Blur Radius", Range(0, 10)) = 2.0
        _Direction  ("Blur Direction (x,y)", Vector) = (1,0,0,0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        
        //GrabPass { }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP 기본 + 장면색 샘플 도우미
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            float  _BlurRadius;
            float4 _Direction; // xy 사용
            CBUFFER_END

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }

            // 간단 가우시안(11탭, 대칭)
            static const float w[6] = {
                0.199, // center
                0.174, 0.121, 0.065, 0.027, 0.009
            };

            half4 frag (Varyings i) : SV_Target
            {
                // 화면UV(0..1)
                float2 uv = GetNormalizedScreenSpaceUV(i.positionCS);

                // 방향(0,0이면 기본 가로)
                float2 dir = _Direction.xy;
                if (abs(dir.x)+abs(dir.y) < 1e-5) dir = float2(1,0);
                dir = normalize(dir);

                // 1픽셀 UV 크기(= 1 / 화면 해상도)
                float2 texel = 1.0 / _ScreenParams.xy;

                // 샘플 간격
                float2 stepUV = dir * _BlurRadius * texel;

                // 중앙 샘플 (RGB만 누적) — 반환형이 3ch이든 4ch이든 캐스팅으로 안전
                half3 rgb = (half3)SampleSceneColor(uv) * w[0];

                // 양방향 누적
                [unroll]
                for (int k = 1; k < 6; k++)
                {
                    float2 o = stepUV * k;
                    rgb += (half3)SampleSceneColor(uv + o) * w[k];
                    rgb += (half3)SampleSceneColor(uv - o) * w[k];
                }

                // 마지막에 알파 붙여 반환
                return half4(rgb, 1);
            }
            ENDHLSL
//            CGPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//            #include "UnityCG.cginc"
//
//            sampler2D _GrabTexture;
//            float4 _GrabTexture_TexelSize;
//            float _BlurRadius;
//
//            struct appdata
//            {
//                float4 vertex : POSITION;
//                float2 uv : TEXCOORD0;
//            };
//
//            struct v2f
//            {
//                float4 vertex : SV_POSITION;
//                float2 uv : TEXCOORD0;
//            };
//
//            v2f vert(appdata v)
//            {
//                v2f o;
//                o.vertex = UnityObjectToClipPos(v.vertex);
//                o.uv = v.uv;
//                return o;
//            }
//
//            static const float kernel[11] = {
//                0.027, 0.065, 0.121, 0.174, 0.199, 0.174, 0.121, 0.065, 0.027, 0.009, 0.003
//            };
//
//            fixed4 frag(v2f i) : SV_Target
//            {
//                float2 uv = float2(i.uv.x, 1.0 - i.uv.y);
//                float2 texelOffset = float2(_GrabTexture_TexelSize.x * _BlurRadius, 0);
//                float4 col = tex2D(_GrabTexture, uv) * kernel[0];
//
//                for (int k = 1; k < 6; ++k)
//                {
//                    float weight = kernel[k];
//                    col += tex2D(_GrabTexture, uv + texelOffset * k) * weight;
//                    col += tex2D(_GrabTexture, uv - texelOffset * k) * weight;
//                }
//
//                return col;
//            }
//            ENDCG
        }
    }

    FallBack "Diffuse"
}
