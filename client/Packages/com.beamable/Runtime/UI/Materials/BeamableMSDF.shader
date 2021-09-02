Shader "Unlit/BeamableMSDF"
{

    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BackgroundTex ("Background Texture", 2D) = "red" {}

        _ImagePx("Image Size", Vector) = (16, 16, 0, 0) // TWO EMPTY SLOTS <-- yw

        _SDF_Threshold("Threshold", Range(0, 1)) = .5
        _SDF_Erosion("Erosion", Range(0, 1)) = .5
        _SDF_Softness("Softness", Range(0, 1)) = .5
        _SDF_StrokeThreshold("Stroke Threshold", Range(0, 1)) = .5
        _SDF_StrokeErosion("Stroke Erosion", Range(0, 1)) = .5

        _SDF_ForegroundColor("Foreground Color", Color) = (1, 1, 1, 1)
        _SDF_StrokeColor("Stroke Color", Color) = (0, 0, 0, 1)
        _SDF_RadiusSize("Radius Size", Range(0, 1)) = 0
        _SDF_BackgroundOpacity("Background Opacity", Range(0, 1)) = 0

        _SDF_DropShadowColor("Drop Shadow Color", Color) = (0,0,0,.34)
        _SDF_DropShadowData("Drop Shadow Data", Vector) = (0, 0, 0, 0)

        _Foreground_Gradient_Start("Foreground Gradient Start", Color) = (1,1,1,1)
        _Foreground_Gradient_End("Foreground Gradient End", Color) = (0,0,0,1)
        _Foreground_Gradient_Amount("Foreground Gradient Amount", Range(0, 1)) = 0
        _Foreground_Gradient_Angle("Foreground Gradient Angle", Range(-3.14, 3.14)) = 0
        _Foreground_Gradient_Offset("Foreground Gradient Offset", Range(-1, 1)) = 0
        _Foreground_Gradient_Scale("Foreground Gradient Scale", Range(0, 2)) = 1

        _Background_Rect("Background Rectangle", Vector) = (.1, .1, .9, .9)
        [Toggle] _Background_PreserveAspect("Background Preserve Aspect", Float) = 0

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
            // make fog work
            #pragma multi_compile_fog

            #include "Standard/MSDF.cginc"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR0;
                float2 unScaledUv: TEXCOORD1;
                float2 aspectRatios: TEXCOORD2;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 color: COLOR0;
                float4 vertex : SV_POSITION;
                float2 unScaledUv : TEXCOORD1;
                float2 backgroundUv : TEXCOORD2;
                float2 aspectRatios : TEXCOORD3;
                float4 data : TEXCOORD4;
            };

            sampler2D _MainTex;
            sampler2D _BackgroundTex;
            float4 _MainTex_ST;
            float4 _BackgroundTex_ST;

            float _SDF_Threshold, _SDF_Erosion, _SDF_Softness;
            float _SDF_StrokeThreshold, _SDF_StrokeErosion;
            float _SDF_RadiusSize;
            float4 _SDF_StrokeColor, _SDF_ForegroundColor;
            float4 _ImagePx;
            float _SDF_BackgroundOpacity;

            float4 _SDF_DropShadowData, _SDF_DropShadowColor;

            float4 _Foreground_Gradient_Start;
            float4 _Foreground_Gradient_End;
            float _Foreground_Gradient_Amount;
            float _Foreground_Gradient_Angle;
            float _Foreground_Gradient_Offset;
            float _Foreground_Gradient_Scale;

            float _Background_PreserveAspect;
            float4 _Background_Rect;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float2 baseUv = float2(0, 0);
                baseUv.x = o.uv.x > 0.5 ? 1 : 0;
                baseUv.y = o.uv.y > 0.5 ? 1 : 0;
                o.unScaledUv = v.unScaledUv;
                o.backgroundUv = TRANSFORM_TEX(v.unScaledUv, _BackgroundTex);
                o.aspectRatios = v.aspectRatios;
                o.color = v.color;
                o.data = float4(v.vertex.z, 0, 0, 0);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float merge(float shape1, float shape2){
                return min(shape1, shape2);
            }
            float intersect(float shape1, float shape2){
                return max(shape1, shape2);
            }
            float round_merge(float shape1, float shape2, float radius){
                float2 intersectionSpace = float2(shape1 - radius, shape2 - radius);
                intersectionSpace = min(intersectionSpace, 0);
                float insideDistance = -length(intersectionSpace);
                float simpleUnion = merge(shape1, shape2);
                float outsideDistance = max(simpleUnion, radius);
                return  insideDistance + outsideDistance;
            }
            float round_intersect(float shape1, float shape2, float radius){
                float2 intersectionSpace = float2(shape1 + radius, shape2 + radius);
                intersectionSpace = max(intersectionSpace, 0);
                float outsideDistance = length(intersectionSpace);
                float simpleIntersection = intersect(shape1, shape2);
                float insideDistance = min(simpleIntersection, -radius);
                return outsideDistance + insideDistance;
            }

            float4 gradient(float2 uv, float4 start, float4 end, float amount, float angle, float offset, float scale) {
                float4 o = 0;

                float2 axis = float2(cos(angle), sin(angle));
                float2 axis2 = float2(-axis.y, axis.x); // orthog vector

                float2 p = (uv -.5 ) * scale;

                // we only care about the value along the axis.
                float x = (dot(axis2, p)) + offset;

                x = clamp(0, 1, x);
                float4 color = lerp(start, end, x);
                color.a *= amount;
                return color;
            }

            float4 premultiplyAlpha(float4 source, float4 dest) {
                float a = source.a + dest.a*(1 - source.a);
                float3 rgb = source.rgb*source.a + dest.rgb*dest.a*(1-source.a);
                rgb /= a+.0000001; // add epislon
                return float4(rgb, a);
            }

            fixed4 SampleSDF(float2 uv) {
                return tex2D(_MainTex, uv);
            }
            fixed4 SampleBackground(float2 uv) {
                return tex2D(_BackgroundTex, uv);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = SampleSDF(i.uv); //tex2D(_MainTex, i.uv);
                fixed4 shadowSample = SampleSDF(i.uv + (_SDF_DropShadowData.xy) * .01);

                float3 dist = 1 - col.rgb;
                float2 toCenter01 = (i.uv * 2) - 1;
                float texDistance = MsdfSignedDistance(1 - col);
                float circleDistance = 1 - CircleDistance(i.uv * 2 -1, 0, (1 - _SDF_RadiusSize) * .5 );

                // MERGE the raw distance with a circular distance, to simulate rounded edges. We need a better approach for this.
                float msdfSigDist = round_merge(texDistance, circleDistance, 0 * 1.4142);

                float bodyAlpha = BasicMSDF2(msdfSigDist, 1, _SDF_Threshold, _SDF_Erosion, _SDF_Softness);
                float outlineAlpha = BasicMSDF2(msdfSigDist, 1, _SDF_StrokeThreshold, _SDF_StrokeErosion, _SDF_Softness);
                outlineAlpha = saturate(((1.0 - bodyAlpha) * outlineAlpha));
                float bodyMask = bodyAlpha > 0;
                float mask = saturate (bodyAlpha + outlineAlpha);

                float4 foreground = bodyAlpha * _SDF_ForegroundColor;
                float4 stroke = outlineAlpha * _SDF_StrokeColor;

                float4 result = 1;

                result.rgba = saturate(foreground + stroke);
                result.a *= mask;

                // SHADOW ----
                float _shadowThreshold = .001; // TODO extract as property
                float4 shadowColor = _SDF_DropShadowColor; // TODO extract as property
                float ShadowBoost = .7; // TODO extract as property

                float texDistanceShadow = MsdfSignedDistance(1 - shadowSample);
                float circleDistanceShadow = 1 - CircleDistance((i.uv+ (_SDF_DropShadowData.xy) * .01) * 2 -1, 0, (1 - _SDF_RadiusSize) * .5 );

                float msdfSigDistShadow = round_merge(texDistanceShadow, circleDistanceShadow, 0 * 1.4142);

                shadowSample = 1 - shadowSample;
                float shadowAlpha = BasicMSDF2(msdfSigDistShadow, 1, _SDF_Threshold + _shadowThreshold, _SDF_Erosion, min(-_SDF_DropShadowData.z, -0.01));
                float shadow = saturate(ShadowBoost * (1.0 - shadowAlpha)) * shadowColor.a;
                result.a += shadow * (1 - mask);
                result.rgb = saturate(lerp(shadowColor.rgb, result.rgb, clamp(0, 1, mask) ));

                // BACKGROUND
                float2 pinMin = _Background_Rect.xy;
                float2 pinMax = _Background_Rect.zw;

                float containerAspect = i.aspectRatios.x;
                float requiredAspectRatio = i.aspectRatios.y;
                float2 existingPinCenter = pinMin + (pinMax - pinMin) * .5;

                float forcedPinWidth =  .5f * abs((1/containerAspect) * requiredAspectRatio * (pinMax.y - pinMin.y));
                float forcedPinHeight =  .5f * abs((containerAspect) *  (pinMax.x - pinMin.x) / requiredAspectRatio);

                float2 forcedPinHalf = float2(
                    clamp(0, 1, lerp(forcedPinWidth, .5 * (pinMax - pinMin).x, lerp(1, containerAspect < requiredAspectRatio, _Background_PreserveAspect))),
                    clamp(0, 1, lerp(forcedPinHeight, .5 * (pinMax - pinMin).y, lerp(1, containerAspect > requiredAspectRatio, _Background_PreserveAspect))));

                pinMin = existingPinCenter - forcedPinHalf;
                pinMax = existingPinCenter + forcedPinHalf;

                float2 pinSize = pinMax - pinMin;
                float2 pinCenter = (pinMin + pinMax) * .5;

                float2 backgroundUv = i.backgroundUv - (pinMin);
                backgroundUv.x /= pinSize.x;
                backgroundUv.y /= pinSize.y;

                fixed4 background = SampleBackground(backgroundUv);
                float backgroundUvMask = (i.unScaledUv.x < pinMax.x) * (i.unScaledUv.x > pinMin.x) * (i.unScaledUv.y < pinMax.y) * (i.unScaledUv.y > pinMin.y) ;
                background *= i.color;
                background.a *= backgroundUvMask;
                float backgroundMix = bodyAlpha * background.a;
                result.rgb = result.rgb * (1 - backgroundMix) + background.rgb * backgroundMix;

                // GRADIENT
                float4 mainGradient = gradient(i.unScaledUv,
                    _Foreground_Gradient_Start,
                    _Foreground_Gradient_End,
                    _Foreground_Gradient_Amount,
                    _Foreground_Gradient_Angle,
                    _Foreground_Gradient_Offset,
                    _Foreground_Gradient_Scale);
                mainGradient.a *= bodyMask;

                return premultiplyAlpha(mainGradient, result);

                return result;
            }
            ENDCG
        }
    }
}
