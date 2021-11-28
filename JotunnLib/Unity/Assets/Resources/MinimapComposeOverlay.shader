Shader "MinimapCompose"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _VanillaTex("Texture", 2D) = "white" {}
    }
        SubShader
        {
            Tags {"Queue"="Transparent" "RenderType"="Transparent" }
            LOD 100

            Pass 
            {
                CGPROGRAM
                #pragma vertex vert 
                #pragma fragment frag
                #pragma target 2.0

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

                sampler2D _MainTex, _BrushTex, _VanillaTex;
                float4 _MainTex_ST;
                fixed4 _Color, _Coordinates;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_MainTex, i.uv);
                    fixed4 col2 = tex2D(_VanillaTex, i.uv);

                    col2.rgba = lerp(col2.rgba, col.rgba, col.a);
                    return col2;
                }
                ENDCG
            }
        }
}