Shader "Game/SpriteHitFlash"
{
    // Sprites/Default + 화이트 플래시 + 냉동 틴트.
    //  _FrostAmount(0~1)만큼 RGB를 _FrostColor로 먼저 lerp(지속: 얼어있는 동안),
    //  그 위에 _FlashAmount(0~1)만큼 _FlashColor로 lerp(순간: 피격). 둘 다 0이면 Sprites/Default와 동일 렌더.
    //  값은 MaterialPropertyBlock으로 렌더러별 주입(제로할당). 플래시·틴트는 같은 블록에 공존.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0,1)) = 0
        _FrostColor ("Frost Color", Color) = (0.55,0.85,1,1)
        _FrostAmount ("Frost Amount", Range(0,1)) = 0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnitySprites.cginc"

            fixed4 _FlashColor;
            float  _FlashAmount;
            fixed4 _FrostColor;
            float  _FrostAmount;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                c.rgb = lerp(c.rgb, _FrostColor.rgb, _FrostAmount); // 냉동 틴트(지속) — 먼저
                c.rgb = lerp(c.rgb, _FlashColor.rgb, _FlashAmount); // 히트 플래시(순간) — 위에 얹음
                c.rgb *= c.a;                                       // Sprites/Default와 동일하게 1회 프리멀티플라이
                return c;
            }
        ENDCG
        }
    }
}
