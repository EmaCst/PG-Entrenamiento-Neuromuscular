Shader "Hidden/Praxen/NatacionCameraOrient"
{
    Properties { _MainTex ("Camera", 2D) = "white" {} _Rotation ("Rotation", Float) = 0 _FlipY ("Flip Y", Float) = 0 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Rotation, _FlipY;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                if (_Rotation == 90) uv = float2(uv.y, 1 - uv.x);
                else if (_Rotation == 180) uv = 1 - uv;
                else if (_Rotation == 270) uv = float2(1 - uv.y, uv.x);
                if (_FlipY > 0.5) uv.y = 1 - uv.y;
                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}
