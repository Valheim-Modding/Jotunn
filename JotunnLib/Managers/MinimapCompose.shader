Shader "Manager/MinimapCompose"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Coordinates("BrushCoordinates", Vector) = (0,0,0,0)
        _Color("BrushColor", Color) = (0,0,0,0)
        _SizeX("BrushSizeX", Float) = 50
        _SizeY("BrushSizeY", Float) = 50
        _BrushTex("BrushTex", 2D) = "white" {}
        _Strength("Strength", Float) = 1
    }
        SubShader
        {
            Tags {"RenderType"="Transparent" }
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

                sampler2D _MainTex, _BrushTex;
                float4 _MainTex_ST;
                fixed4 _Color, _Coordinates;
                float _Strength, _SizeX, _SizeY;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    half4 col = tex2D(_MainTex, i.uv);
                    half2 diff = _Coordinates.xy - i.uv;
                    half2 scaledDiff = diff * half2(_SizeX,_SizeY);
                    if (abs(scaledDiff.x) < 0.5 && abs(scaledDiff.y) < 0.5)
                    {
                        half4 texVal = tex2D(_BrushTex, scaledDiff + half2(0.5, 0.5));
                        return saturate(texVal.w > 0.01 ? _Color * texVal * _Strength + col : col);
                    }
                    else
                    {
                        return col;
                    }
                }
                ENDCG
            }
        }
}