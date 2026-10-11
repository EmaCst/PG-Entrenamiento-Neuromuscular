Shader "Praxen/ViewerDistortion"
{
    Properties { _MainTex ("Stereo frame", 2D) = "black" {} }
    SubShader
    {
        Pass
        {
            ZTest Always ZWrite Off Cull Off Blend Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _ImageScale, _LensSpacing, _VerticalOffset, _Curvature;
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_img v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                float eye = i.uv.x < 0.5 ? 0.0 : 1.0;
                float2 p = float2((i.uv.x * 2.0 - eye) * 2.0 - 1.0, i.uv.y * 2.0 - 1.0);
                p.x -= eye < 0.5 ? -_LensSpacing : _LensSpacing;
                p.y -= _VerticalOffset;
                float2 q = p / _ImageScale;
                q *= 1.0 + _Curvature * dot(q, q);
                if (abs(q.x) > 1.0 || abs(q.y) > 1.0) return half4(0, 0, 0, 1);
                float2 uv = float2((eye + (q.x + 1.0) * 0.5) * 0.5, (q.y + 1.0) * 0.5);
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0) uv.y = 1.0 - uv.y;
                #endif
                return half4(tex2D(_MainTex, uv).rgb, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
