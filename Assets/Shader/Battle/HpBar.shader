Shader "UI/HPBar"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Fill ("Fill 0~1", Range(0,1)) = 1
        _startAngle ("Start Angle", Range(0, 360)) = 0
        _arcAngle("Arc Angle", Range(0, 360)) = 360
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 col : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float  _Fill;
            float _startAngle;
            float _arcAngle;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = v.color * _Color;
                return o;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.col;

                float2 dir = i.uv - float2(0.5, 0.5);

                // 각도 0~360
                float angle = degrees(atan2(dir.y, dir.x));
                angle = angle < 0 ? angle + 360 : angle;

                float start = _startAngle;
                float arc   = _arcAngle;
                float fillEnd = fmod(start + arc * _Fill, 360);

                // 1) 기본 아크 마스크
                float normalMask = step(start, angle) * step(angle, fillEnd);
                float wrapMask   = step(angle, fillEnd) + (1 - step(angle, start));
                float wrapCond   = step(fillEnd, start);           // fillEnd < start ? 1 : 0
                float mask       = lerp(normalMask, saturate(wrapMask), wrapCond);

                // 2) 빈 아크(arc == 0) 이거나 fill == 0 이면 0
                //    부동소수 비교 대신 작은 epsilon 사용
                const float EPS = 0.0001;
                float hasArc  = step(EPS, arc);      // arc > EPS 면 1
                float hasFill = step(EPS, _Fill);    // _Fill > EPS 면 1
                float notEmpty = hasArc * hasFill;   // 둘 다 있어야 1
                mask *= notEmpty;

                // 3) 꽉 찬 원 (_Fill == 1 && arc == 360) 이면 무조건 1
                //    이것도 epsilon으로 처리
                float isFullFill   = step(1.0 - EPS, _Fill);       // _Fill ~ 1
                float isFullCircle = step(360.0 - 0.01, arc);      // arc ~ 360
                float forceFull    = isFullFill * isFullCircle;
                mask = lerp(mask, 1.0, forceFull);

                c.rgb *= mask;
                c.a   *= mask;

                return c;
            }
            ENDCG
        }
    }
}