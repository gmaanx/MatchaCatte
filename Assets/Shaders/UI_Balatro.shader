Shader "UI/BalatroBackground"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Balatro Vortex Settings)]
        _SpinRotationSpeed ("Spinning Speed", Float) = 1.5
        _MoveSpeed ("Move Speed", Float) = 3.0
        _Offset ("Offset (XY)", Vector) = (0, 0, 0, 0)
        _Contrast ("Contrast", Float) = 2.0
        _Lighting ("Lighting", Float) = 0.5
        _SpinAmount ("Spinning Force", Float) = 0.25
        _SpinEase ("Spinning Ease", Float) = 1.0
        _PixelFilter ("Pixelation", Float) = 740.0
        [Toggle] _IsRotating ("Is Rotating", Float) = 1.0

        [Header(Colors)]
        _Color1 ("Color 1 (Sáng nhất/Highlight)", Color) = (0.455, 0.286, 0.647, 1)      
        _Color2 ("Color 2 (Màu nền/Base)", Color) = (0.267, 0.133, 0.380, 1)    
        _Color3 ("Color 3 (Tối nhất/Shadow)", Color) = (0.090, 0.043, 0.141, 1)      

        [Header(Effects)]
        _Grayscale ("Grayscale Blend", Range(0,1)) = 0.0
        _GalaxyBlend ("Galaxy Blend", Range(0,1)) = 0.0
        _Glitch ("Glitch Amount", Range(0,1)) = 0.0

        [Header(Galaxy Colors)]
        _Color1_Alt ("Galaxy Dark", Color) = (0.3, 0.22, 0.29, 1)      
        _Color2_Alt ("Galaxy Base", Color) = (0.6, 0.45, 0.59, 1)    
        _Color3_Alt ("Galaxy Highlight", Color) = (1.0, 0.745, 0.984, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
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

            fixed4 _Color, _Color1, _Color2, _Color3;
            fixed4 _Color1_Alt, _Color2_Alt, _Color3_Alt;
            
            float _SpinRotationSpeed;
            float _MoveSpeed;
            float2 _Offset;
            float _Contrast;
            float _Lighting;
            float _SpinAmount;
            float _SpinEase;
            float _PixelFilter;
            float _IsRotating;

            float _Grayscale, _GalaxyBlend, _Glitch;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float2 random2(float2 p) {
                return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
            }

            // --- Cắt ngang để lấy Glitch UV Tearing ---
            float2 applyGlitch(float2 uv, float glitchAmount) {
                if (glitchAmount == 0.0) return uv;
                
                // Mở rộng mảng nhiễu ra to hơn
                float split = step(0.6, sin(uv.y * 15.0 + _Time.y * 30.0));
                float split2 = step(0.7, sin(uv.y * 8.0 - _Time.y * 25.0));
                
                // Tăng độ xé hình lên 20% màn hình
                uv.x += split * 0.15 * glitchAmount * sin(_Time.y * 100.0);
                uv.x -= split2 * 0.20 * glitchAmount * cos(_Time.y * 80.0);
                
                // Chớp giật lên xuống
                uv.y += split * 0.02 * glitchAmount * sin(_Time.y * 150.0);
                
                return uv;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                IN.texcoord = applyGlitch(IN.texcoord, _Glitch);

                float2 screenSize = float2(1920.0, 1080.0);
                float2 screen_coords = IN.texcoord * screenSize;
                
                float pixel_size = length(screenSize) / _PixelFilter;
                
                if (_Glitch > 0.0)
                {
                    float tear = frac(sin(dot(float2(floor(IN.texcoord.y * 30.0), _Time.y * 5.0), float2(12.9898, 78.233))) * 43758.5453);
                    if (tear > 0.8)
                    {
                        screen_coords.x += (tear - 0.9) * 2.0 * _Glitch * screenSize.x;
                    }
                }


                float2 uv = (floor(screen_coords / pixel_size) * pixel_size - 0.5 * screenSize) / length(screenSize) - _Offset;
                float uv_len = length(uv);

                float speed = (_SpinRotationSpeed * _SpinEase * 0.2);
                if (_IsRotating > 0.5)
                {
                    speed = _Time.y * speed;
                }
                speed += 302.2;

                float new_pixel_angle = atan2(uv.y, uv.x) + speed - _SpinEase * 20.0 * (1.0 * _SpinAmount * uv_len + (1.0 - 1.0 * _SpinAmount));
                float2 mid = (screenSize / length(screenSize)) / 2.0;
                uv = float2(uv_len * cos(new_pixel_angle) + mid.x, uv_len * sin(new_pixel_angle) + mid.y) - mid;

                uv *= 30.0;
                speed = _Time.y * _MoveSpeed;
                float2 uv2 = float2(uv.x + uv.y, uv.x + uv.y);

                for (int i = 0; i < 5; i++)
                {
                    uv2 += sin(max(uv.x, uv.y)) + uv;
                    uv  += 0.5 * float2(cos(5.1123314 + 0.353 * uv2.y + speed * 0.131121), sin(uv2.x - 0.113 * speed));
                    uv  -= 1.0 * cos(uv.x + uv.y) - 1.0 * sin(uv.x * 0.711 - uv.y);
                }

                float contrast_mod = (0.25 * _Contrast + 0.5 * _SpinAmount + 1.2);
                float paint_res = min(2.0, max(0.0, length(uv) * 0.035 * contrast_mod));
                
                float c1p = max(0.0, 1.0 - contrast_mod * abs(1.0 - paint_res));
                float c2p = max(0.0, 1.0 - contrast_mod * abs(paint_res));
                float c3p = 1.0 - min(1.0, c1p + c2p);
                
                float ligth = (_Lighting - 0.2) * max(c1p * 5.0 - 4.0, 0.0) + _Lighting * max(c2p * 5.0 - 4.0, 0.0);
                
                fixed4 c1 = lerp(_Color1, _Color1_Alt, _GalaxyBlend);
                fixed4 c2 = lerp(_Color2, _Color2_Alt, _GalaxyBlend);
                fixed4 c3 = lerp(_Color3, _Color3_Alt, _GalaxyBlend);

                float4 finalColor = (0.3 / _Contrast) * c1 + (1.0 - 0.3 / _Contrast) * (c1 * c1p + c2 * c2p + float4(c3p * c3.rgb, c3p * c1.a)) + ligth;

                float crackIntensity = 0.0;
                float holeMask = 0.0;
                float litiEffect = 0.0;
                float edgeGlow = 0.0;
                float3 crackBaseColor = float3(1.0, 0.1, 0.8);
                float2 shardOffset = float2(0,0);

                if (_GalaxyBlend > 0.0)
                {
                    float2 uvCenter = IN.texcoord - 0.5;
                    uvCenter.x *= (1920.0 / 1080.0);
                    
                    // Hiệu ứng vuốt từ trái sang phải (hoàn thành nhanh gấp đôi)
                    float sweepBlend = saturate(_GalaxyBlend * 2.0);
                    float sweepX = lerp(-1.5, 1.5, sweepBlend);
                    float sweepMask = smoothstep(uvCenter.x, uvCenter.x + 0.2, sweepX);

                    // Đường nứt trên: Dốc LÊN về bên phải, tần số nhiễu riêng
                    float crackBaseTop = uvCenter.x * 0.15; 
                    crackBaseTop += (abs(frac(uvCenter.x * 2.0) - 0.5) - 0.25) * 0.3;
                    crackBaseTop += (abs(frac(uvCenter.x * 5.5 + 0.7) - 0.5) - 0.25) * 0.1;
                    crackBaseTop += (abs(frac(uvCenter.x * 15.0 + 2.1) - 0.5) - 0.25) * 0.03;

                    // Đường nứt dưới: Dốc XUỐNG về bên phải, tần số nhiễu hoàn toàn khác
                    float crackBaseBot = -uvCenter.x * 0.1;
                    crackBaseBot += (abs(frac(uvCenter.x * 1.2 + 0.4) - 0.5) - 0.25) * 0.4;
                    crackBaseBot += (abs(frac(uvCenter.x * 3.8 + 1.2) - 0.5) - 0.25) * 0.15;
                    crackBaseBot += (abs(frac(uvCenter.x * 9.0 + 0.5) - 0.5) - 0.25) * 0.04;

                    // Khe hở mở rộng từ trái sang phải
                    float crackOpening = 0.08 * sweepMask;

                    float topCrack = crackBaseTop + crackOpening;
                    float bottomCrack = crackBaseBot - crackOpening;

                    // Chống lồng chéo (Anti-crossing): Đẩy 2 đường nứt ra xa nhau nếu chúng chạm mặt
                    float overlapFix = max(0.0, (bottomCrack - topCrack) * 0.5 + 0.02 * sweepMask);
                    topCrack += overlapFix;
                    bottomCrack -= overlapFix;

                    // Vùng giữa 2 vết nứt (chỉ xuất hiện theo sweepMask)
                    holeMask = step(uvCenter.y, topCrack) * step(bottomCrack, uvCenter.y) * sweepMask;

                    // Tính khoảng cách tới mép nứt
                    float crackDist = min(abs(uvCenter.y - topCrack), abs(uvCenter.y - bottomCrack));

                    // Phát sáng vết nứt (Thu nhỏ viền lại cực sắc)
                    float crackThickness = 0.002 * sweepMask; // Rất mỏng
                    float crackGlow = (1.0 - smoothstep(0.0, crackThickness * 3.0, crackDist));
                    float crackCore = (1.0 - smoothstep(0.0, crackThickness * 0.5, crackDist));
                    
                    // Tạo sự đứt đoạn (Breakup) dựa trên độ sáng/tối của pattern background gốc
                    float bgLum = dot(finalColor.rgb, float3(0.299, 0.587, 0.114));
                    float edgeBreakup = smoothstep(0.1, 0.6, bgLum);
                    
                    crackIntensity = (crackGlow * 1.5 + crackCore * 3.0) * sweepMask * (0.1 + 1.5 * edgeBreakup);

                    // Màu sáng viền: Tím tối galaxy (Dark Galaxy Purple)
                    crackBaseColor = float3(0.4, 0.0, 0.8);

                    // Refraction: Bóp méo nhẹ bề mặt kính còn sót lại
                    shardOffset = float2(0.05, 0.0) * smoothstep(0.2, 0.0, crackDist) * sweepMask;
                    shardOffset *= (1.0 - holeMask);
                    screen_coords += shardOffset * screenSize;

                    // Hạt bụi tròn nhỏ liti (Round dust particles) bay lơ lửng
                    float2 shardUV = uvCenter * 30.0 + _Time.y * float2(0.1, 0.05); // Scale to hơn để hạt nhỏ lại, trôi chậm hơn
                    float2 i_shard = floor(shardUV);
                    float2 f_shard = frac(shardUV);
                    float2 p_shard = random2(i_shard); // Điểm ngẫu nhiên trong mỗi ô
                    
                    // Hình tròn (Round) thay vì thoi
                    float dustDist = length(f_shard - p_shard);
                    
                    // Rất thưa thớt (Sparse)
                    float shardMaskVal = step(0.95, random2(i_shard + 5.0).x);
                    
                    // Vệt sáng cực nhỏ như hạt bụi
                    float shardVis = (1.0 - smoothstep(0.0, 0.05, dustDist)) * shardMaskVal;
                    
                    // Chỉ hiện ở trong khu vực lỗ hổng đen và chạy theo sweepMask
                    litiEffect = shardVis * holeMask;
                }
                
                if (_Grayscale > 0.0)
                {
                    float lum = dot(finalColor.rgb, float3(0.299, 0.587, 0.114));
                    finalColor.rgb = lerp(finalColor.rgb, float3(lum, lum, lum), _Grayscale);
                }

                if (_GalaxyBlend > 0.0)
                {
                    // Phần ở giữa tối đi thay vì đen thui
                    finalColor.rgb = lerp(finalColor.rgb, finalColor.rgb * 0.2, holeMask);

                    // Viền nứt tia sét gốc
                    float3 totalNeon = crackBaseColor * crackIntensity;
                    finalColor.rgb += totalNeon * 4.0;

                    // Các mảnh kính nhỏ liti bay lấp lánh (màu trắng sáng)
                    finalColor.rgb += float3(1.0, 1.0, 1.0) * litiEffect * 3.0;
                }



                if (_Glitch > 0.0)
                {
                    float sline = sin(IN.texcoord.y * 500.0 - _Time.y * 30.0);
                    finalColor.rgb -= sline * 0.1 * _Glitch;
                    
                    float staticNoise = frac(sin(dot(IN.texcoord + _Time.y, float2(12.9898, 78.233))) * 43758.5453);
                    finalColor.rgb += (staticNoise - 0.5) * 0.4 * _Glitch;
                    
                    finalColor.r += 0.1 * _Glitch;
                    finalColor.b -= 0.1 * _Glitch;
                }

                return finalColor * IN.color;
            }
            ENDCG
        }
    }
}