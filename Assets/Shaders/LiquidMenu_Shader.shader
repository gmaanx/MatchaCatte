Shader "UI/LiquidMenu"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        
        [Header(Balatro Shader Settings)]
        _SpinRotationSpeed ("Spin Rotation Speed", Float) = 2.0
        _MoveSpeed ("Move Speed", Float) = 7.0
        _Offset ("Offset (XY)", Vector) = (0, 0, 0, 0)
        
        _Color1 ("Color 1", Color) = (0.35, 0.65, 0.35, 1.0)   // Xanh Matcha sáng
        _Color2 ("Color 2", Color) = (0.1, 0.4, 0.25, 1.0)    // Xanh lục sẫm
        _Color3 ("Color 3", Color) = (0.05, 0.15, 0.1, 1.0)   // Đen ánh xanh
        
        _Contrast ("Contrast", Float) = 3.5
        _Lighting ("Lighting", Float) = 0.4
        _SpinAmount ("Spin Amount", Float) = 0.25
        _PixelFilter ("Pixel Filter", Float) = 740.0
        [Toggle] _IsRotating ("Is Rotating", Float) = 1.0
        
        [Header(Effects)]
        _Grayscale ("Grayscale Blend", Range(0,1)) = 0.0
        _Glitch ("Glitch Amount", Range(0,1)) = 0.0
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "CanUseSpriteAtlas"="True" 
        }
        
        Cull Off 
        Lighting Off 
        ZWrite Off 
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            
            float _SpinRotationSpeed;
            float _MoveSpeed;
            float2 _Offset;
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float _Contrast;
            float _Lighting;
            float _SpinAmount;
            float _PixelFilter;
            float _IsRotating;
            float _Grayscale;
            float _Glitch;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                return OUT;
            }

            float2 applyGlitch(float2 uv, float glitchAmount) {
                if (glitchAmount == 0.0) return uv;
                
                float split = step(0.6, sin(uv.y * 15.0 + _Time.y * 30.0));
                float split2 = step(0.7, sin(uv.y * 8.0 - _Time.y * 25.0));
                
                uv.x += split * 0.15 * glitchAmount * sin(_Time.y * 100.0);
                uv.x -= split2 * 0.20 * glitchAmount * cos(_Time.y * 80.0);
                uv.y += split * 0.02 * glitchAmount * sin(_Time.y * 150.0);
                
                return uv;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                IN.texcoord = applyGlitch(IN.texcoord, _Glitch);

                float2 screenSize = float2(1920.0, 1080.0);
                float2 screen_coords = IN.texcoord * screenSize;
                
                float pixel_size = length(screenSize) / _PixelFilter;
                
                // Căn chỉnh pixelate và dời tâm về giữa
                float2 uv = (floor(screen_coords / pixel_size) * pixel_size - 0.5 * screenSize) / length(screenSize) - _Offset;
                float uv_len = length(uv);

                // Tính toán gia tốc xoáy
                float speed = (_SpinRotationSpeed * 1.0 * 0.2);
                if (_IsRotating > 0.5)
                {
                    speed = _Time.y * speed;
                }
                speed += 302.2;

                // Xoắn ốc (Vortex)
                float new_pixel_angle = atan2(uv.y, uv.x) + speed - 1.0 * 20.0 * (1.0 * _SpinAmount * uv_len + (1.0 - 1.0 * _SpinAmount));
                float2 mid = (screenSize / length(screenSize)) / 2.0;
                uv = float2(uv_len * cos(new_pixel_angle) + mid.x, uv_len * sin(new_pixel_angle) + mid.y) - mid;

                uv *= 30.0;
                speed = _Time.y * _MoveSpeed;
                float2 uv2 = float2(uv.x + uv.y, uv.x + uv.y);

                // Fractal lượn sóng
                for (int i = 0; i < 5; i++)
                {
                    uv2 += sin(max(uv.x, uv.y)) + uv;
                    uv  += 0.5 * float2(cos(5.1123314 + 0.353 * uv2.y + speed * 0.131121), sin(uv2.x - 0.113 * speed));
                    uv  -= 1.0 * cos(uv.x + uv.y) - 1.0 * sin(uv.x * 0.711 - uv.y);
                }

                // Tính toán tương phản và trộn màu
                float contrast_mod = (0.25 * _Contrast + 0.5 * _SpinAmount + 1.2);
                float paint_res = min(2.0, max(0.0, length(uv) * 0.035 * contrast_mod));
                
                float c1p = max(0.0, 1.0 - contrast_mod * abs(1.0 - paint_res));
                float c2p = max(0.0, 1.0 - contrast_mod * abs(paint_res));
                float c3p = 1.0 - min(1.0, c1p + c2p);

                float ligth = (_Lighting - 0.2) * max(c1p * 5.0 - 4.0, 0.0) + _Lighting * max(c2p * 5.0 - 4.0, 0.0);
                
                float4 ret_col = (0.3 / _Contrast) * _Color1 + (1.0 - 0.3 / _Contrast) * (_Color1 * c1p + _Color2 * c2p + float4(c3p * _Color3.rgb, c3p * _Color1.a)) + ligth;
                
                if (_Grayscale > 0.0)
                {
                    float lum = dot(ret_col.rgb, float3(0.299, 0.587, 0.114));
                    ret_col.rgb = lerp(ret_col.rgb, float3(lum, lum, lum), _Grayscale);
                }
                
                if (_Glitch > 0.0)
                {
                    float sline = sin(IN.texcoord.y * 500.0 - _Time.y * 30.0);
                    ret_col.rgb -= sline * 0.1 * _Glitch;
                    
                    float staticNoise = frac(sin(dot(IN.texcoord + _Time.y, float2(12.9898, 78.233))) * 43758.5453);
                    ret_col.rgb += (staticNoise - 0.5) * 0.4 * _Glitch;
                    
                    ret_col.r += 0.2 * _Glitch;
                    ret_col.b -= 0.2 * _Glitch;
                }

                return ret_col * IN.color;
            }
            ENDCG
        }
    }
}