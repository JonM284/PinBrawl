Shader "Custom/Shadow"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Pass
        {
            Name "Outline"
            Tags { "RenderType"="Opaque" "Queue"="Geometry+2"}
            
            Stencil{
                Ref 5
                Comp Greater
                pass Replace 
            }
             
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
            
            CULL Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;
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
                return _Color;
            }
                
            ENDCG
        }
    }
    FallBack "Diffuse"
}