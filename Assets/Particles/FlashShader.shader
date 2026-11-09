Shader "Custom/SpriteFlash"
{
    Properties
    {
        // PerRendererData로 설정해 스프라이트 렌더러가 자신의 텍스처를 전달하도록 함
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _FlashColor;
            float _FlashAmount;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // 스프라이트 색 틴트 적용
                float4 col = tex2D(_MainTex, i.uv) * i.color;
                col.rgb = lerp(col.rgb, _FlashColor.rgb, _FlashAmount);
                return col;
            }
            ENDCG
        }
    }
}

