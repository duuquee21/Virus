Shader "Custom/GradienteFondo"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1,0,0,1)
        _Speed ("Speed", Float) = 0.2
        _Scale ("Gradient Scale", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }

        Pass
        {
            HLSLPROGRAM
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _MainColor;
            float _Speed;
            float _Scale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Movimiento del degradado
                float t = i.uv.y * _Scale + _Time.y * _Speed;

                // Onda suave
                float gradient = abs(sin(t));

                // Mezcla entre negro y color
                return lerp(float4(0,0,0,1), _MainColor, gradient);
            }
            ENDHLSL
        }
    }
}
