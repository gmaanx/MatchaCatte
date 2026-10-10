Shader "Hidden/UI_CircleTransition"
{
    Properties
    {
        _Radius ("Radius", Range(0, 1.5)) = 1.5
        _Color ("Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            float _Radius;
            fixed4 _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Màn hình có tỷ lệ (aspect ratio), ta cần nhân UV.x với aspect để hình tròn không bị méo thành oval
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 uv = i.texcoord - 0.5;
                uv.x *= aspect;
                
                float dist = length(uv);
                
                // Cắt thủng vòng tròn (Alpha = 1 nếu ngoài vùng radius, Alpha = 0 nếu trong vùng radius)
                float alpha = smoothstep(_Radius - 0.05, _Radius, dist);
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}
