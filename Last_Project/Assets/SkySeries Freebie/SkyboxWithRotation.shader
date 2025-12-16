Shader "Custom/SkyboxWithRotationXY"
{
    Properties
    {
        _Cubemap("Cubemap", Cube) = "" {}
        _RotationX("Rotation X (Degrees)", Range(0, 360)) = 0
        _RotationY("Rotation Y (Degrees)", Range(0, 360)) = 0
        _Exposure("Exposure", Range(0, 8)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }

        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _Cubemap;
            float _RotationX;
            float _RotationY;
            float _Exposure;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // 스카이박스 큐브 방향 벡터
                o.dir = normalize(v.vertex.xyz);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float radX = radians(_RotationX);
                float radY = radians(_RotationY);
                float sX = sin(radX);
                float cX = cos(radX);
                float sY = sin(radY);
                float cY = cos(radY);

                // X, Y축 회전 행렬
                float3x3 rotX = float3x3(
                    1, 0, 0,
                    0, cX, -sX,
                    0, sX, cX
                );

                float3x3 rotY = float3x3(
                    cY, 0, sY,
                    0, 1, 0,
                    -sY, 0, cY
                );

                // 두 회전 행렬을 적용하여 최종 방향 벡터 구하기
                float3 dir = mul(rotY, mul(rotX, i.dir));

                // 큐브맵 샘플링 및 노출 적용
                fixed4 col = texCUBE(_Cubemap, dir) * _Exposure;
                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
