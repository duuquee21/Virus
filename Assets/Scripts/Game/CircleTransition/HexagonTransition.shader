Shader "Custom/ShapeTransition"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Radius ("Size", Range(0.0, 1.5)) = 0
        _CenterX ("Center X", Range(0.0, 1.0)) = 0.5
        _CenterY ("Center Y", Range(0.0, 1.0)) = 0.5
        _Aspect ("Aspect Ratio", Float) = 1.77
        _Shape ("Shape (0:Circ, 1:Hex, 2:Pent)", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Radius, _CenterX, _CenterY, _Aspect;
            int _Shape;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Distancia Círculo
            float sdCircle(float2 p, float r) { return length(p) - r; }

            // Distancia Hexágono
            float sdHexagon(float2 p, float r) {
                const float3 k = float3(-0.866025404, 0.5, 0.577350269);
                p = abs(p);
                p -= 2.0 * min(dot(k.xy, p), 0.0) * k.xy;
                p -= float2(clamp(p.x, -k.z * r, k.z * r), r);
                return length(p) * sign(p.y);
            }

            // Distancia Pentágono
            float sdPentagon(float2 p, float r) {
                const float3 k = float3(0.809016994, 0.587785252, 0.726542528);
                p.x = abs(p.x);
                p -= 2.0 * min(dot(float2(-k.x, k.y), p), 0.0) * float2(-k.x, k.y);
                p -= 2.0 * min(dot(float2(k.x, k.y), p), 0.0) * float2(k.x, k.y);
                p -= float2(clamp(p.x, -r * k.z, r * k.z), r);
                return length(p) * sign(p.y);
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.uv - float2(_CenterX, _CenterY);
                uv.x *= _Aspect;

                float d = 0;
                if (_Shape == 0) d = sdCircle(uv, _Radius);
                else if (_Shape == 1) d = sdHexagon(uv, _Radius);
                else d = sdPentagon(uv, _Radius);

                return fixed4(_Color.rgb, smoothstep(0.0, 0.01, d));
            }
            ENDCG
        }
    }
}