Shader "VFX/VFX_TexNiuQuAlphaRemapCol"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] 
        _Cull("Cull Mode", Float) = 2
        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcBlend("src Blend",int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)]
        _DstBlend("DstBlend",int) = 10
    //-----------------------------------------------------------------------------
       [Toggle(_USEPARTICLECUSTOMDATA)]_USEPARTICLECUSTOMDATA("UseParticleCustomData", Float) = 1
       [Toggle(_USEREMAP)]_USEREMAP("UseRemap", Float) = 1
        [NoScaleOffset]_RemapColTex("RemapColTex", 2D) = "white" {}
        _RemapColTexTillingOffset("RemapColTexTillingOffset", Vector) = (1, 1, 0, 0)
        _RemapColTexMoveSpeed("RemapColTexMoveSpeed", Vector) = (0, 0, 0, 0)

        [Toggle(_ALPHAUSER)]_ALPHAUSER("AlphaUseR", Float) = 1
        [HDR]_MainColor("MainColor", Color) = (1, 1, 1, 1)
        [NoScaleOffset]_MainTex("MainTex", 2D) = "white" {}
        _MainTexTillingOffset("MainTexTillingOffset", Vector) = (1, 1, 0, 0)
        _MainTexSpped("MainTexSpped", Vector) = (0, 0, 0, 0)
        _MainTexNiuQuStrenth("MainTexNiuQuStrenth", Vector) = (0, 0, 0, 0)

        [Toggle(_USEALPHAPART)]_USEALPHAPART("UseAlphaPart", Float) = 1
        [Toggle(_ALPHATEXUSER)]_ALPHATEXUSER("AlphaTexUseR", Float) = 1
        [NoScaleOffset]_AlphaTex("AlphaTex", 2D) = "white" {}
        _AlphaTexTillingOffset("AlphaTexTillingOffset", Vector) = (1, 1, 0, 0)
        _AlphaTexMoveSpeed("AlphaTexMoveSpeed", Vector) = (0, 0, 0, 0)
        _AlphaNiuQuStrength("AlphaNiuQuStrength", Vector) = (0, 0, 0, 0)

        [Toggle(_USERONGJIE)]_USERONGJIE("UseRongJie", Float) = 1
        [Toggle(_USERONGJIETEXR)]_USERONGJIETEXR("UseRongJieTexR", Float) = 1
        [NoScaleOffset]_RongJieTex("RongJieTex", 2D) = "white" {}
        _RongJieTexTillingOffset("RongJieTexTillingOffset", Vector) = (1, 1, 0, 0)
        _RongJieTexSpeed("RongJieTexSpeed", Vector) = (0, 0, 0, 0)
        _RongJieTexNiuQuStrength("RongJieTexNiuQuStrength", Vector) = (0, 0, 0, 0)
        _RongJieMaxCountMul("RongJieMaxCountMul", Float) = 1
        _RongJieStep("RongJieStep", Range(0, 1)) = 0
        [HDR]_GouBianCol("GouBianCol", Color) = (1, 1, 1, 1)
        _RongJieGouBianWidth("RongJieGouBianWidth", Range(0, 1)) = 0

        [Toggle(_USENIUQU)]_USENIUQU("UseNiuQu", Float) = 1
        [Toggle(_USER)]_USER("UseR", Float) = 1
        [NoScaleOffset]_NiuQuTex("NiuQuTex", 2D) = "white" {}
        _NiuQuTexTillingOffset("NiuQuTexTillingOffset", Vector) = (1, 1, 0, 0)
        _NiuQuTexMoveSpeed("NiuQuTexMoveSpeed", Vector) = (0, 0, 0, 0)

        _AlphaPow("AlphaPow", Float) = 1
        _AlphaMul("AlphaMul", Float) = 1
        _FinalAlpha("FinalAlpha", Range(0, 1)) = 1
    }
        SubShader
        {
             Tags
            {
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
            }
            LOD 100

            Pass
            {
                Blend[_SrcBlend][_DstBlend]
                Cull[_Cull]
                ZWrite Off

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma shader_feature _USEPARTICLECUSTOMDATA
            #pragma shader_feature _USEREMAP
            #pragma shader_feature _ALPHAUSER

            #pragma shader_feature _USEALPHAPART
            #pragma shader_feature _ALPHATEXUSER

            #pragma shader_feature _USERONGJIE
            #pragma shader_feature _USERONGJIETEXR

            #pragma shader_feature _USENIUQU
            #pragma shader_feature _USER

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 vertexCol : COLOR;
                float4 uv0 : TEXCOORD0;
#ifdef _USEPARTICLECUSTOMDATA
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
#endif
            };
            struct v2f
            {
                float4 uv0 : TEXCOORD0;
#ifdef _USEPARTICLECUSTOMDATA
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 vertexCol:TEXCOORD3;
                UNITY_FOG_COORDS(4)
#else
                float4 vertexCol:TEXCOORD1;
                UNITY_FOG_COORDS(2)
#endif
                float4 vertex : SV_POSITION;
            };

        #ifdef _USEREMAP
            sampler2D _RemapColTex;
            float4 _RemapColTexTillingOffset;
            float2 _RemapColTexMoveSpeed;
        #endif

            float4 _MainColor;
            sampler2D _MainTex;
            float4 _MainTexTillingOffset;
            float2 _MainTexSpped;
            float2 _MainTexNiuQuStrenth;

        #ifdef _USEALPHAPART
            sampler2D _AlphaTex;
            float4 _AlphaTexTillingOffset;
            float2 _AlphaTexMoveSpeed;
            float2 _AlphaNiuQuStrength;
        #endif

        #ifdef _USERONGJIE
            sampler2D _RongJieTex;
            float4 _RongJieTexTillingOffset;
            float2 _RongJieTexSpeed;
            float2 _RongJieTexNiuQuStrength;
            float _RongJieMaxCountMul;
            float _RongJieStep;
            float4 _GouBianCol;
            float _RongJieGouBianWidth;
        #endif

        #ifdef _USENIUQU
            sampler2D _NiuQuTex;
            float4 _NiuQuTexTillingOffset;
            float2 _NiuQuTexMoveSpeed;
        #endif

            float _AlphaPow, _AlphaMul, _FinalAlpha;
               

            float4 MySamplerTex(float2 uv,sampler2D InputTex,float4 TexTilling,float2 TexMoveSpeed,float2 NiuQu,float2 CustomData)
            {
                float2 vec2 = uv * TexTilling.xy + TexTilling.zw;
                vec2 += _Time.y * TexMoveSpeed;
                vec2 += NiuQu;
                vec2 += CustomData;
                float4 col = tex2D(InputTex, vec2);
                return col;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); 
                o.vertexCol = v.vertexCol;
                o.uv0 = v.uv0;
#ifdef _USEPARTICLECUSTOMDATA
                o.uv1 = v.uv1;
                o.uv2 = v.uv2;
#endif
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 MainTexUVCustomData = 0;
                float2 RongJieTexUVCustomData = 0;
                float2 NiuQuTexUVCustomData = 0;
                float RongJieStepCustomData = 0;
            #ifdef _USEPARTICLECUSTOMDATA
                 MainTexUVCustomData = i.uv0.zw;
                 RongJieTexUVCustomData = i.uv1.xy;
                 NiuQuTexUVCustomData = i.uv1.zw;
                 RongJieStepCustomData = i.uv2.x;
            #endif

                float2 uv = i.uv0.xy;
                float nStrength = 0;


            #ifdef _USENIUQU
                float4 nCol = MySamplerTex(uv, _NiuQuTex, _NiuQuTexTillingOffset, _NiuQuTexMoveSpeed, float2(0, 0), NiuQuTexUVCustomData);
                nStrength = nCol.a;
                #ifdef _USER
                    nStrength = nCol.r;
                #endif
            #endif

                float4 col = MySamplerTex(uv,_MainTex, _MainTexTillingOffset, _MainTexSpped,_MainTexNiuQuStrenth* nStrength, MainTexUVCustomData);
            #ifdef _ALPHAUSER                
                    col.a = col.r;
            #endif
                col *= _MainColor;
             
                col *= i.vertexCol;
                col.a = pow(col.a, _AlphaPow);

            #ifdef _USEREMAP
                float4 remapCol = MySamplerTex(uv, _RemapColTex, _RemapColTexTillingOffset, _RemapColTexMoveSpeed, float2(0, 0), float2(0, 0));
                remapCol.a = 1;
                col *= remapCol;
            #endif

            #ifdef _USEALPHAPART
                float4 alphaCol = MySamplerTex(uv,_AlphaTex, _AlphaTexTillingOffset, _AlphaTexMoveSpeed, _AlphaNiuQuStrength* nStrength, float2(0, 0));
                float alphaPartA = alphaCol.a;
                #ifdef _ALPHATEXUSER
                    alphaPartA = alphaCol.r;
                #endif
                col.a *= alphaPartA;
            #endif

            #ifdef _USERONGJIE

                float value1 = max(RongJieStepCustomData,_RongJieStep) - 0.01;
                float value2 = saturate(value1* _RongJieMaxCountMul);
                float4 rongJieCol = MySamplerTex(uv, _RongJieTex, _RongJieTexTillingOffset, _RongJieTexSpeed, _RongJieTexNiuQuStrength* nStrength, RongJieTexUVCustomData);
                float value3 = rongJieCol.a;
                #ifdef _USERONGJIETEXR
                    value3 = rongJieCol.r;
                #endif
                float aFactor1 = smoothstep(value1, value2, value3);
                col.a *= aFactor1;

                float gValue1 = smoothstep(0, 0.01, _RongJieStep) * _RongJieGouBianWidth + value1;
                float gValue2 = saturate(saturate(gValue1) * _RongJieMaxCountMul);
                float aFactor2 = smoothstep(gValue1, gValue2, value3);

                float aFactor3 = saturate(aFactor1 - aFactor2);
                col.rgb += _GouBianCol.rgb * aFactor3;
            #endif
                col.a = saturate(col.a * _AlphaMul) * _FinalAlpha;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
