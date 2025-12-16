Shader "Custom/SkyboxWithSunRotation"
{
    Properties
    {
        _MainTex ("Skybox Cubemap", Cube) = "_Skybox" { }
        _Sun ("Sun Texture", 2D) = "white" { }
        _SunRotation ("Sun Rotation", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Background" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // 필요한 변수들
            float _SunRotation; // 태양 회전 값
            samplerCUBE _MainTex; // Cubemap 텍스처
            sampler2D _Sun; // 태양 텍스처

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float3 normal : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Cubemap 텍스처 샘플링 (하늘 배경)
                half4 color = texCUBE(_MainTex, i.normal);

                // 태양 텍스처를 회전시키기 위한 처리
                float3 sunDirection = float3(sin(_SunRotation), 1.0, cos(_SunRotation));
                half4 sunColor = tex2D(_Sun, float2(0.5f, 0.5f)); // 텍스처 좌표는 임의로 설정

                // 태양의 위치에 따른 색상 변화
                return lerp(color, sunColor, 0.5); // 하늘과 태양 색상 blending
            }
            ENDCG
        }
    }
}
