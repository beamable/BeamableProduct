Shader "Unlit/BeamableSDF"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BackgroundTexture ("Background", 2D) = "white" {}
        
        [Toggle(MULTISAMPLING)] _SDF_MULTISAMPLING("Multisampling", Float) = 1
        _SDF_SamplingDistance("Sampling Distance", Range(0, .1)) = .01
        
        [HideInInspector]_StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask("ColorMask", Float) = 15
    }
    SubShader
    {
        LOD 100

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }
        
        ColorMask RGBA

        Pass
        {
            ZWrite Off
            ZTest[unity_GUIZTestMode]
            Cull Back
            AlphaTest Greater .01
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature MULTISAMPLING

            #include "UnityCG.cginc"
            #include "Packages\com.beamable\Runtime\UI\Materials\SDF\SDFFunctions.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 size : TEXCOORD1;
                float2 params : TEXCOORD2;
                float2 coords : TEXCOORD3;
                float4 color : COLOR;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 sizeNCoords : TEXCOORD1;
                float3 roundingNThresholdNOutlineWidth : TEXCOORD3;
                float3 shadowOffset : TEXCOORD4;
                float4 shadowColor : TEXCOORD5;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 outlineColor : NORMAL;
                float2 uvToCoordsFactor : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _BackgroundTexture;
            float4 _BackgroundTexture_ST;
            
            float _SDF_SamplingDistance;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(float3(v.vertex.x, v.vertex.y, 0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.sizeNCoords.xy = v.size;
                o.sizeNCoords.zw = v.coords;
                o.outlineColor.rgb = floatToRGB(v.params.y);
                o.outlineColor.a = 1;
                o.roundingNThresholdNOutlineWidth.x = v.vertex.z;
                o.roundingNThresholdNOutlineWidth.y = v.normal.x;
                o.roundingNThresholdNOutlineWidth.z = v.params.x;
                o.shadowColor.rgb = floatToRGB(v.tangent.w);
                float2 temp = floatToRGB(v.tangent.z).xy;
                o.shadowColor.a = temp.y;
                o.shadowOffset = v.tangent.rgb;
                o.shadowOffset.z = temp.x;
                o.uvToCoordsFactor = v.normal.yz;
                return o;
            }
            
            // return full rect distance
            float getRectDistance(float2 coords, float2 size, float rounding){
                coords = coords - float2(.5, .5);
                coords *= size;
                float rectDist = sdfRoundedRectangle(coords, size * .5, rounding);
                return rectDist;
            }

            // returns SDF image distance
            float getDistance(float2 uv, float2 coords, float2 size, float rounding){
                float dist = tex2D(_MainTex, uv).a;
                #if MULTISAMPLING
                dist += tex2D(_MainTex, float2(uv.x - _SDF_SamplingDistance, uv.y - _SDF_SamplingDistance)).a;
                dist += tex2D(_MainTex, float2(uv.x + _SDF_SamplingDistance, uv.y - _SDF_SamplingDistance)).a;
                dist += tex2D(_MainTex, float2(uv.x - _SDF_SamplingDistance, uv.y + _SDF_SamplingDistance)).a;
                dist += tex2D(_MainTex, float2(uv.x + _SDF_SamplingDistance, uv.y + _SDF_SamplingDistance)).a;
                dist += tex2D(_MainTex, float2(uv.x - _SDF_SamplingDistance, uv.y)).a;
                dist += tex2D(_MainTex, float2(uv.x + _SDF_SamplingDistance, uv.y)).a;
                dist += tex2D(_MainTex, float2(uv.x, uv.y + _SDF_SamplingDistance)).a;
                dist += tex2D(_MainTex, float2(uv.x, uv.y + _SDF_SamplingDistance)).a;
                dist /= 9;
                #endif
                dist = .5 - dist;
                return dist;
            }
            
            // returns intersection of SDF image distance and rect distance
            float getMergedDistance(float2 uv, float2 coords, float2 size, float rounding){
                return max(getDistance(uv, coords, size, rounding), getRectDistance(coords, size, rounding));
            }
            
            // returns SDF value with given threshold
            float calculateValue(float dist, float threshold){
                return 1 - aaStep(threshold, dist);
            }
            
            // returns main color
            float4 mainColor(v2f i){
                float4 color = i.color;
                color *= tex2D(_BackgroundTexture, float2(i.uv.x, i.uv.y));
                return color;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 size = i.sizeNCoords.xy;
                float2 coords = i.sizeNCoords.zw;
                float rounding = i.roundingNThresholdNOutlineWidth.x;
                float threshold = i.roundingNThresholdNOutlineWidth.y;
                float outlineWidth = i.roundingNThresholdNOutlineWidth.z;
                
                // Main color
                float dist = getMergedDistance(i.uv, coords, size, rounding);
                float mainColorValue = calculateValue(dist, threshold);
                float4 main = mainColor(i);
                // Outline
                float outlineValue = calculateValue(dist, threshold + outlineWidth);
                float4 outline = i.outlineColor;
                outline.a *= main.a * outlineValue;
                main.a *= mainColorValue;
                // Blending main and outline
                float4 final;
                final.rgb = lerp(outline, main, main.a / outline.a);
                final.a = max(outline.a, main.a);
                
                // Shadow
                float shadowDist = getMergedDistance(i.uv - i.shadowOffset.xy, coords - i.shadowOffset.xy / size, size, rounding);
                float shadowValue = calculateValue(shadowDist, threshold + i.shadowOffset.z);
                i.shadowColor.a *= shadowValue;
                final.rgb = lerp(i.shadowColor.rgb, saturate(final.rgb), saturate(final.a / (main.a + 0.001)));
                final.a += i.shadowColor.a;
                
                return final;
            }
            ENDCG
        }
    }
}
