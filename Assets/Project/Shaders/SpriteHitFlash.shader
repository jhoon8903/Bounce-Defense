Shader "Game/SpriteHitFlash"
{
    // Sprites/Default + 화이트 플래시. _FlashAmount(0~1)만큼 RGB를 _FlashColor로 lerp.
    // amount=0이면 Sprites/Default와 동일 렌더. 값은 MaterialPropertyBlock으로 렌더러별 주입(제로할당).
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0,1)) = 0
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

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                c.rgb = lerp(c.rgb, _FlashColor.rgb, _FlashAmount); // 스트레이트 RGB를 플래시색으로
                c.rgb *= c.a;                                       // Sprites/Default와 동일하게 1회 프리멀티플라이
                return c;
            }
        ENDCG
        }
    }
}
