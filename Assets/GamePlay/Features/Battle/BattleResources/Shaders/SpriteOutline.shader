Shader "Custom/Sprite_Outline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,0,0,1)
        _OutlineSize ("Outline Size", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _OutlineSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float alpha = 0.0;
                float2 offsets[8] = {
                    float2(-1, -1), 
                    float2(-1, 0), 
                    float2(-1, 1),
                    float2(0, -1),                
                    float2(0, 1),     //주변 8픽셀 감지
                    float2(1, -1),  
                    float2(1, 0), 
                    float2(1, 1)
                };

                for (int j = 0; j < 8; ++j)
                {
                    float2 offsetUV = i.uv + offsets[j] * _OutlineSize * _MainTex_TexelSize.xy;
                    fixed4 sample = tex2D(_MainTex, offsetUV);
                    alpha = max(alpha, sample.a);
                }

                // 현재 픽셀 알파
                fixed4 center = tex2D(_MainTex, i.uv);
                if (center.a > 0.01)
                    discard; // 내부 픽셀은 버림

                return fixed4(_OutlineColor.rgb, alpha * _OutlineColor.a);
            }
            ENDCG
        }
    }
}
