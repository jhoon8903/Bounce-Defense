Shader "Game/SpriteHitFlash"
{
    // URP 2D Sprite-Unlit 기반 + 화이트 플래시(순간) + 냉동 틴트(지속).
    //  _FrostAmount(0~1)만큼 RGB를 _FrostColor로 먼저 lerp(지속: 얼어있는 동안),
    //  그 위에 _FlashAmount(0~1)만큼 _FlashColor로 lerp(순간: 피격). 둘 다 0이면 URP 스프라이트와 동일 렌더.
    //  모든 프로퍼티가 UnityPerMaterial CBUFFER에 있어 SRP 배처 적격 → 적 블록·몹이 볼/캐릭과 함께 배칭됨.
    //  값은 EnemyView가 렌더러별 인스턴스 머티리얼에 SetFloat/SetColor(풀 재사용, 제로할당). MPB 금지(SRP 배처 깸).
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0,1)) = 0
        _FrostColor ("Frost Color", Color) = (0.55,0.85,1,1)
        _FrostAmount ("Frost Amount", Range(0,1)) = 0
        [MaterialToggle] _ZWrite ("ZWrite", Float) = 0

        // 레거시 폴백 프로퍼티(URP 스프라이트 셰이더와 동일 — Sprites/Default 폴백 대비).
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        // URP 2D Sprite-Unlit과 동일 블렌드. (레거시 Blend One OneMinusSrcAlpha + 프리멀티플과 화면 결과 동일:
        //  둘 다 rgb*a + dst*(1-a). SrcAlpha 블렌드가 알파를 곱하므로 프래그에서 프리멀티플 불필요.)
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment HitFlashFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            // GPU Instancing (URP 기본 스프라이트와 동일 variant 세트 유지 → 배칭 풀 정합).
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // SRP 배처 조건: 모든 머티리얼 프로퍼티를 단일 UnityPerMaterial CBUFFER에, ifdef 금지(레이아웃 고정).
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _FlashColor;
                half  _FlashAmount;
                half4 _FrostColor;
                half  _FrostAmount;
            CBUFFER_END

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 HitFlashFragment(Varyings input) : SV_Target
            {
                half4 c = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                c.rgb = lerp(c.rgb, _FrostColor.rgb, _FrostAmount); // 냉동 틴트(지속) — 먼저
                c.rgb = lerp(c.rgb, _FlashColor.rgb, _FlashAmount); // 히트 플래시(순간) — 위에 얹음
                return c;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
