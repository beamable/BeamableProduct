Shader "Unlit/BeamBackgroundUnlit"
{
    
      Properties
    {
        _MainTex ("Texture (unused)", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Color1 ("Color 1", Color) = (1,0,0,1)
        _Color2 ("Color 2", Color) = (0,1,0,1)
        _Color3 ("Color 3", Color) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Tint;
            float4 _Color1, _Color2, _Color3;

            // Unity time: _Time.y is time in seconds
            // _ScreenParams.x = width, .y = height
            // We'll use _Time.y as iTime and _ScreenParams.xy as iResolution

            float3 mod289(float3 x) {
                return x - floor(x * (1.0 / 289.0)) * 289.0;
            }

            float4 mod289(float4 x) {
                return x - floor(x * (1.0 / 289.0)) * 289.0;
            }

            float4 permute(float4 x) {
                return mod289(((x*34.0)+1.0)*x);
            }

            float4 taylorInvSqrt(float4 r)
            {
                return 1.79284291400159 - 0.85373472095314 * r;
            }

            float snoise(float3 v)
            {
                const float2 C = float2(1.0/6.0, 1.0/3.0);
                const float4 D = float4(0.0, 0.5, 1.0, 2.0);

                // First corner
                float3 i  = floor(v + dot(v, C.yyy) );
                float3 x0 =   v - i + dot(i, C.xxx);

                // Other corners
                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1.0 - g;
                float3 i1 = min(g.xyz, l.zxy);
                float3 i2 = max(g.xyz, l.zxy);

                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy;
                float3 x3 = x0 - D.yyy;

                // Permutations
                i = mod289(i);
                float4 p = permute( permute( permute(
                    i.z + float4(0.0, i1.z, i2.z, 1.0 ))
                  + i.y + float4(0.0, i1.y, i2.y, 1.0 ))
                  + i.x + float4(0.0, i1.x, i2.x, 1.0 ));

                float n_ = 0.142857142857; // 1.0/7.0
                float3 ns = n_ * D.wyz - D.xzx;

                float4 j = p - 49.0 * floor(p * ns.z * ns.z);

                float4 x_ = floor(j * ns.z);
                float4 y_ = floor(j - 7.0 * x_ );

                float4 x = x_ * ns.x + ns.yyyy;
                float4 y = y_ * ns.x + ns.yyyy;
                float4 h = 1.0 - abs(x) - abs(y);

                float4 b0 = float4( x.xy, y.xy );
                float4 b1 = float4( x.zw, y.zw );

                float4 s0 = floor(b0)*2.0 + 1.0;
                float4 s1 = floor(b1)*2.0 + 1.0;
                float4 sh = -step(h, float4(0.0,0.0,0.0,0.0));

                float4 a0 = float4(b0.x, b0.z, b0.y, b0.w); // xzyw reorder equivalent to .xzyw
                a0 = a0 + float4(s0.x, s0.z, s0.y, s0.w) * float4(sh.x, sh.x, sh.y, sh.y);
                float4 a1 = float4(b1.x, b1.z, b1.y, b1.w);
                a1 = a1 + float4(s1.x, s1.z, s1.y, s1.w) * float4(sh.z, sh.z, sh.w, sh.w);

                // Reconstruct p0..p3 properly
                float3 p0 = float3(a0.x, a0.y, h.x);
                float3 p1 = float3(a0.z, a0.w, h.y);
                float3 p2 = float3(a1.x, a1.y, h.z);
                float3 p3 = float3(a1.z, a1.w, h.w);

                // Normalise gradients
                float4 norm = taylorInvSqrt(float4(dot(p0,p0), dot(p1,p1), dot(p2,p2), dot(p3,p3)));
                p0 *= norm.x;
                p1 *= norm.y;
                p2 *= norm.z;
                p3 *= norm.w;

                // Mix final noise value
                float4 m = max(0.6 - float4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
                m = m * m;
                return 42.0 * dot( m*m, float4( dot(p0,x0), dot(p1,x1),
                                                dot(p2,x2), dot(p3,x3) ) );
            }

            float normnoise(float noise) {
                return 0.5*(noise+1.0);
            }

            float clouds(float2 uv, float iTime) {
                uv += float2(iTime*0.05, + iTime*0.01);

                float2 off1 = float2(50.0,33.0);
                float2 off2 = float2(0.0, 0.0);
                float2 off3 = float2(-300.0, 50.0);
                float2 off4 = float2(-100.0, 200.0);
                float2 off5 = float2(400.0, -200.0);
                float2 off6 = float2(100.0, -1000.0);
                float scale1 = 3.0;
                float scale2 = 6.0;
                float scale3 = 12.0;
                float scale4 = 24.0;
                float scale5 = 48.0;
                float scale6 = 96.0;

                float n =  normnoise(
                    snoise(float3((uv+off1)*scale1, iTime*0.5)) * 0.8 +
                    snoise(float3((uv+off2)*scale2, iTime*0.4)) * 0.4 +
                    snoise(float3((uv+off3)*scale3, iTime*0.1)) * 0.2 +
                    snoise(float3((uv+off4)*scale4, iTime*0.7)) * 0.1 +
                    snoise(float3((uv+off5)*scale5, iTime*0.2)) * 0.05 +
                    snoise(float3((uv+off6)*scale6, iTime*0.3)) * 0.025
                );

                return n;
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // iResolution mapping
                float2 iResolution = float2(_ScreenParams.x, _ScreenParams.y);
                float iTime = _Time.y * .1;

                // fragCoord in pixels
                float2 fragCoord = i.uv * iResolution;
                // emulate original uv = fragCoord.xy / iResolution.x;
                float2 uv = fragCoord / iResolution.x;

                float2 center = float2(0.5, 0.5 * (iResolution.y / iResolution.x));

                float2 light1 = float2(sin(iTime*1.2+50.0)*1.0 + cos(iTime*0.4+10.0)*0.6,
                                       sin(iTime*1.2+100.0)*0.8 + cos(iTime*0.2+20.0)*-0.2) * 0.2 + center;
                float3 lightColor1 = _Color1.rgb;

                float2 light2 = float2(sin(iTime+3.0)*-2.0, cos(iTime+7.0)*1.0) * 0.2 + center;
                float3 lightColor2 = _Color2.rgb;

                float2 light3 = float2(sin(iTime+3.0)*2.0, cos(iTime+14.0)*-1.0) * 0.2 + center;
                float3 lightColor3 = _Color3.rgb;

                float cloudIntensity1 = 1*(1.0 - (1.5 * distance(uv, light1)));
                                                cloudIntensity1 = saturate(cloudIntensity1);

                float lighIntensity1 = 5.0 / (1 + (50.0 * distance(uv, light1)));
                // lighIntensity1 = min(lighIntensity1, .9);
                lighIntensity1 = smoothstep(0, 1, lighIntensity1);
                float cloudIntensity2 = .7*(1.0 - (2.5 * distance(uv, light2)));
                
                float lighIntensity2 = 5.0 / (1 + 70.0 * distance(uv, light2));
                                lighIntensity2 = saturate(lighIntensity2);

                lighIntensity2 = smoothstep(0, 1, lighIntensity2);

                float cloudIntensity3 = 1*(1.0 - (2.5 * distance(uv, light3)));
                cloudIntensity3 = saturate(cloudIntensity3);
                
                float lighIntensity3 = 4.0 / (1 + 80.0 * distance(uv, light3));
                lighIntensity3 = smoothstep(0, 1, lighIntensity3);

                float c = clouds(uv, iTime);

                // float3 color = (cloudIntensity1 * c) * lightColor1 + lighIntensity1 * lightColor1 + (cloudIntensity2 * c) * lightColor2 + lighIntensity2 * lightColor2
                ;
                // + (cloudIntensity3 * c) * lightColor3 + lighIntensity3 * lightColor3 ;

                // color = saturate(color);
                // color =  (cloudIntensity2 * c) * lightColor2 + lighIntensity2 * lightColor2 ;
                float3 color =
                    (cloudIntensity1 * c) * lightColor1 + lighIntensity1 * lightColor1 +
                    (cloudIntensity2 * c) * lightColor2 + lighIntensity2 * lightColor2 +
                    (cloudIntensity3 * c) * lightColor3 + lighIntensity3 * lightColor3;

                color = saturate(color);
                color = color.rgb / (1.2 + color.rgb);

                // color.rgb = lerp(color.rgb, c * _Color1.rgb, .4);
                // apply tint if desired
                color *= _Tint.rgb;

                return float4(color, 1.0);
            }

            ENDCG
        }
    }
}
