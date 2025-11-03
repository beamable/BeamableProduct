Shader "Unlit/BeamBackgroundUnlit"
{
    
    // taken from: https://www.shadertoy.com/view/tXlXDX
  Properties
    {
        _TimeScale("Time Scale", Float) = 1.0
        _Brightness("Brightness", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _TimeScale;
            float _Brightness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 cos3(float3 v) { return float3(cos(v.x), cos(v.y), cos(v.z)); }
            float3 sin3(float3 v) { return float3(sin(v.x), sin(v.y), sin(v.z)); }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 I = i.uv * 2.0 - 1.0;
                float t = _Time.y * _TimeScale;

                float3 O = 0;
                float i_iter = 0;
                float z = 0;
                float d = 0;

                // Raymarch loop
                for (i_iter = 0; i_iter < 70.0; i_iter++)
                {
                    float3 p = z * normalize(float3(I + I, 0) - float3(1,1,1));

                    // Twist with depth
                    float cs = cos((z + t) * 0.1);
                    float sn = sin((z + t) * 0.1);
                    float2x2 rot = float2x2(cs, -sn, sn, cs);
                    p.xy = mul(rot, p.xy);

                    // Scroll forward
                    p.z -= 5.0 * t;

                    // Turbulence loop
                    d = 1.0;
                    float3 tp = p;
                    for (int j = 0; j < 8; j++)
                    {
                        tp += cos3(tp.yzx * d + t) / d;
                        d /= .6 + (1+sin(_Time.x * .1))*.3;
                    }

                    // Distance to irregular gyroid
                    float val = abs(2.0 - dot(cos3(tp), sin3(tp.yzx * 0.6))) / 8.0;
                    d = 0.02 + val;
                    z += d;

                    // Add color/glow
                    O += float3(2.0, z/4.0, 2.5) / d;
                }

                // Tanh tonemapping
                O = tanh(O * O / 1e7) * _Brightness;

                return float4(O, 1.0);
            }
            ENDCG
        }
    }
}
