Shader "Unlit/BeamLibraryGradientUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RectPosition ("RectPosition", Vector) = (0,0,.1,.1)
        _Size ("Size", Vector) = (0,0,1,1)
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _RectPosition;
            float4 _Size;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

                float x = _RectPosition.x / _Size.z;
                x += i.uv.x * (_RectPosition.z / _Size.z);

                float y = _RectPosition.y / _Size.w;
                y += (1-i.uv.y) * (_RectPosition.w / _Size.w);

                if (y < .1/_Size.w)
                {
                    discard;
                }

                // dark purple
                fixed4 c1 = fixed4(.1,.2,.4,1);
                fixed4 c2 = fixed4(.3,.1,.4,1);
                
                fixed4 r1 = fixed4(.25,.2,.4,1);
                fixed4 r2 = fixed4(.2,0,.2,1);

                // greenish blue
                // fixed4 c1 = fixed4(.1,.5,.4,1);
                // fixed4 c2 = fixed4(.1,.4,.5,1);
                //
                // fixed4 r1 = fixed4(.25,.4,.4,1);
                // fixed4 r2 = fixed4(.3,.4,.8,1);

                fixed4 col = lerp(c1, c2, x);
                fixed4 colR = lerp(r1, r2, x);
                col = lerp(col, colR, y);

                
                return col;
            }
            ENDCG
        }
    }
}
