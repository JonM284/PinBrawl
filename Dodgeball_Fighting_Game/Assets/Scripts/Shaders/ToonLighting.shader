Shader "Custom/ToonLighting"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainColor ("Tint", Color) = (1,1,1,1)
        _RampTex ("Texture", 2D) = "white" {}
        _LightInt ("Light Instensity", Range(0,1)) = 1
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness", Range(0.001, 0.05)) = 0.1
    }
    SubShader
    {
        LOD 100

        Name "Toon Textured Shader"
        
        CGINCLUDE
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal_world : TEXCOORD1;
                float3 vertex_world : TEXCOORD2;
            };

        ENDCG
        
        Pass
        {
            
            Name "Toon Shading"
            Tags { "RenderType"="Opaque" "Queue"="Geometry+2"}
            LOD 100
        
            Stencil{
                Ref 6
                Comp gequal   
                Pass replace
            }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog


            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _MainColor;
            sampler2D _RampTex;
            float4 _RampTex_ST;
            half _LightInt;
            half4 _LightColor0;

            float3 LambertShading(float3 colorRefl, float lightInt, float3 normal, float3 lightDir)
			{
				return colorRefl * lightInt * max(0.1, dot(normal, lightDir));
			}

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal_world = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0))).xyz;
                o.vertex_world = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float3 normal = i.normal_world;
				//Direction of Directional environmental lighting
				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
				//Take only the rgb values of the light color (directional environmental lighting)
				fixed3 colorRefl = _LightColor0.rgb;
				float3 diffuse = LambertShading(colorRefl, _LightInt, normal, lightDir);
                float2 diffuseUV = diffuse.xy;
                fixed4 ramp = tex2D(_RampTex, diffuseUV);
                col.rgb *= _MainColor;
                return col * ramp;
            }
            ENDCG
        }
        
        Pass
        {
            Name "Outline"
            Tags { "RenderType"="Opaque" "Queue"="Geometry+2"}
            
            Stencil{
                Ref 6
                Comp notequal
                pass keep 
            }
            CULL Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            
            float4 _OutlineColor;
            float _OutlineThickness;
            
            v2f vert (appdata v)
            {
                v2f o;
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 norm = normalize(mul((float3x3)unity_MatrixITMV, v.normal));
                float2 offset = TransformViewToProjection(norm.xy);
                o.vertex.xy += offset * _OutlineThickness;
                o.normal_world = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0))).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
                
            ENDCG
        }
    }
}
