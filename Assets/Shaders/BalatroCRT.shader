Shader "UI/BalatroCRT"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        
        [Header(TV Effects (URP Safe))]
        _Scanline ("Scanline Opacity", Range(0, 1)) = 0.15
        _ScanlineFreq ("Scanline Frequency", Float) = 800.0
        _ScanlineSpeed ("Scanline Speed", Float) = 0.05
        _Noise ("Static Noise", Range(0, 1)) = 0.06
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Overlay+100" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
        }
        Cull Off Lighting Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            // Đã tiễn biến _Vignette ra chuồng gà
            float _Scanline, _ScanlineFreq, _ScanlineSpeed, _Noise;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.texcoord;
                OUT.color = IN.color;
                return OUT;
            }

            float rand(float2 co) {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.uv;

                // Sọc ngang
                float scanline = sin((uv.y + _Time.y * _ScanlineSpeed) * _ScanlineFreq) * 0.5 + 0.5;
                float scanAlpha = scanline * _Scanline;

                // Nhiễu hột
                float noiseAlpha = rand(uv * _Time.y) * _Noise;

                // Chỉ trộn sọc và nhiễu, dẹp màng đen
                float finalAlpha = clamp(scanAlpha + noiseAlpha, 0.0, 1.0);

                return fixed4(0, 0, 0, finalAlpha) * IN.color;
            }
            ENDCG
        }
    }
}