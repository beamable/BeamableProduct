Shader "Unlit/BeamBackgroundUnlit"
{
    
    Properties
    {
            _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color1 ("Color 1", Color) = (1,0,0,1)
        _Color2 ("Color 2", Color) = (0,1,0,1)
        _Color3 ("Color 3", Color) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZWrite Off
            Cull Off
            Lighting Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color1, _Color2, _Color3;
            sampler2D _MainTex;
            float4 _MainTex_ST; // optional, for tiling/offset

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR0;
            };

            float4 _Resolution;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // ---------------- Helpers ----------------
            #define S(a,b,t) smoothstep(a,b,t)

            float distLine(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a;
                float2 ba = b - a;
                float t = clamp(dot(pa, ba)/dot(ba, ba), 0.0, 1.0);
                return length(pa - ba*t);
            }

            float li(float2 p, float2 a, float2 b)
            {
                float d = distLine(p,a,b);
                float m = S(0.03,0.01,d);
                float d2 = length(a-b);
                m *= S(1.2,0.8,d2)*0.5 + S(0.05,0.03,abs(d2-0.75));
                return m;
            }

            float distTriangle(float2 p, float2 p0, float2 p1, float2 p2)
            {
                float2 e0 = p1 - p0;
                float2 e1 = p2 - p1;
                float2 e2 = p0 - p2;

                float2 v0 = p - p0;
                float2 v1 = p - p1;
                float2 v2 = p - p2;

                float2 pq0 = v0 - e0*clamp(dot(v0,e0)/dot(e0,e0),0.0,1.0);
                float2 pq1 = v1 - e1*clamp(dot(v1,e1)/dot(e1,e1),0.0,1.0);
                float2 pq2 = v2 - e2*clamp(dot(v2,e2)/dot(e2,e2),0.0,1.0);

                float s = sign(e0.x*e2.y - e0.y*e2.x);
                float2 d = min(min(float2(dot(pq0,pq0), s*(v0.x*e0.y-v0.y*e0.x)),
                                   float2(dot(pq1,pq1), s*(v1.x*e1.y-v1.y*e1.x))),
                               float2(dot(pq2,pq2), s*(v2.x*e2.y-v2.y*e2.x)));

                return -sqrt(d.x)*sign(d.y);
            }

            float tri(float2 p, float2 a, float2 b, float2 c)
            {
                float d = distTriangle(p,a,b,c);
                float m = S(0.03,0.01,d);
                float d2 = length(a-b);
                m *= S(1.2,0.8,d2)*0.5 + S(0.05,0.03,abs(d2-0.75));
                return m;
            }

            float N21(float2 p)
            {
                p = frac(p * float2(233.34, 851.73));
                p += dot(p,p+23.45);
                return frac(p.x*p.y);
            }

            float2 N22(float2 p)
            {
                float n = N21(p);
                return float2(n, N21(p+n));
            }

            float2 getPos(float2 id, float2 offset, float time)
            {
                float2 n = N22(id+offset) * time;
                return offset + sin(n)*0.4;
            }

            float layer(float2 uv, float time)
            {
                float2 gv = frac(uv) - 0.5;
                float2 id = floor(uv);

                float2 p[9];
                int i=0;
                for(float y=-1.0;y<=1.0;y++)
                    for(float x=-1.0;x<=1.0;x++)
                        p[i++] = getPos(id,float2(x,y), time);

                float t = time*10.0;
                float m=0.0;
                for(int i=0;i<9;i++)
                {
                    m += li(gv,p[4],p[i]);

                    float2 j = (p[i]-gv)*20.0;
                    float sparkle = 1.0/dot(j,j);
                    m += sparkle*(sin(t + frac(p[i].x)*10.0)*0.5+0.5);

                    for(int yi=i+1;yi<9;yi++)
                        for(int zi=yi+1;zi<9;zi++)
                        {
                            float len1 = abs(length(p[i]-p[yi]));
                            float len2 = abs(length(p[yi]-p[zi]));
                            float len3 = abs(length(p[i]-p[zi]));
                            if(len1+len2+len3 < 2.8)
                                m += tri(gv,p[i],p[yi],p[zi])*0.3;
                        }
                }

                m += li(gv,p[1],p[3]);
                m += li(gv,p[1],p[5]);
                m += li(gv,p[7],p[3]);
                m += li(gv,p[7],p[5]);

                return m;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 res = float2(512, 512);
                float2 uv = (i.uv*res.xy - 0.5*res.xy)/res.y;
                float time = _Time.y * .25 + 1000;
                float m=0.0;
                float t = time*0.1;

                float gradient = uv.y;

                float s = sin(t);
                float c = cos(t);
                float2x2 rot = float2x2(c,-s,s,c);
                uv = mul(rot,uv);

                for(float iLayer=0.0;iLayer<1.0;iLayer+=1.0/3.0)
                {
                    float z = frac(iLayer+t);
                    float size = lerp(10.0,0.5,z);
                    float fade = S(0.0,0.5,z)*S(1.0,0.8,z);
                    m += layer(uv*size + iLayer*20.0, time)*fade;
                }

                // Color fade between primaries
                float3 colors[3];
                colors[0] = _Color1.xyz;
                colors[1] = _Color2.xyz;
                colors[2] = _Color3.xyz;

                float t_mod = fmod(t,3.0);
                int idx = (int)floor(t_mod);
                int nextIdx = (idx+1)%3;
                float blend = frac(t_mod);

                float3 base = lerp(colors[idx], colors[nextIdx], blend);

                float3 col = m*base;
                col -= gradient*base;

                float4 tex = tex2D(_MainTex, i.uv);
                tex.rgb *= .8;
                tex.a = .8;
                float4 final = (float4(col,1.0) * i.color);// * lerp(.3, .7, i.uv.y);
                final.a = 1;
                final.rgb *= lerp(.9, 1, i.uv.y);
                return final;
            }

            ENDCG
        }
    }
}
