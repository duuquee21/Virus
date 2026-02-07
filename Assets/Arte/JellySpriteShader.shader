Shader "Custom/JellyMultiImpact" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            // Arrays para manejar hasta 4 impactos simultáneos
            // x, y = posición local | z = deformación | w = radio/suavizado
            float4 _Impacts[4]; 

            v2f vert(appdata_t v) {
                v2f o;
                float2 totalDeform = float2(0,0);
                float2 dirFromCenter = normalize(v.vertex.xy);

                for(int i = 0; i < 4; i++) {
                    float2 impactPos = _Impacts[i].xy;
                    float deformAmount = _Impacts[i].z;
                    
                    float distToImpact = distance(v.vertex.xy, impactPos);
                    float radius = 0.5;
                    float normalizedDist = saturate(distToImpact / radius);
                    float falloff = 1.0 - smoothstep(0.0, 1.0, normalizedDist);
                    
                    float impactDistFromCenter = length(impactPos);
                    float vertexDistFromCenter = length(v.vertex.xy);
                    float directionFactor = (impactDistFromCenter > vertexDistFromCenter) ? -1.0 : 1.0;

                    totalDeform += dirFromCenter * (falloff * falloff * (3.0 - 2.0 * falloff) * deformAmount * directionFactor);
                }

                v.vertex.xy += totalDeform;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return tex2D(_MainTex, i.texcoord) * i.color;
            }
            ENDCG
        }
    }
}