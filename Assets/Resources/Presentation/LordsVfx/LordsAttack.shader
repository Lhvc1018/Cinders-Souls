Shader "Cinders/Lords Attack Mask"
{
 Properties { _MainTex("Lords mask",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Blend SrcAlpha One
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_ST; float4 _Tint;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   Varyings vert(Attributes input){Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.uv=TRANSFORM_TEX(input.uv,_MainTex);o.color=input.color*_Tint;return o;}
   half4 frag(Varyings i):SV_Target {half mask=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).r;return half4(i.color.rgb,i.color.a*mask);}
   ENDHLSL
  }
 }
}
