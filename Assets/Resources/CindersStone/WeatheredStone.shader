Shader "Cinders/Weathered Stone"
{
 Properties
 {
  _BaseMap("Stone colour",2D)="white"{}
  _NormalMap("Stone relief",2D)="bump"{}
  _RoughMap("Roughness",2D)="white"{}
  _BaseColor("Tint",Color)=(0.65,0.65,0.62,1)
  _Scale("Repeats per metre",Float)=0.35
  _NormalStrength("Relief strength",Range(0,2))=0.7
  _Damp("Dampness",Range(0,1))=0.2
 }
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Name "ForwardLit"
   Tags{"LightMode"="UniversalForwardOnly"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.5
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
   TEXTURE2D(_RoughMap);SAMPLER(sampler_RoughMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor;float _Scale,_NormalStrength,_Damp;
   CBUFFER_END
   struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct Varyings{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;half fog:TEXCOORD2;};
   Varyings vert(Attributes i){Varyings o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.fog=ComputeFogFactor(p.positionCS.z);return o;}
   half4 frag(Varyings i):SV_Target
   {
    float3 n=normalize(i.normalWS),w=pow(abs(n),8);w/=max(dot(w,1),.0001);float3 p=i.positionWS*_Scale;
    half3 a=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.zy).rgb*w.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*w.z;
    half rough=SAMPLE_TEXTURE2D(_RoughMap,sampler_RoughMap,p.zy).r*w.x+SAMPLE_TEXTURE2D(_RoughMap,sampler_RoughMap,p.xz).r*w.y+SAMPLE_TEXTURE2D(_RoughMap,sampler_RoughMap,p.xy).r*w.z;
    half3 nx=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,p.zy),_NormalStrength);
    half3 ny=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,p.xz),_NormalStrength);
    half3 nz=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,p.xy),_NormalStrength);
    float3 detail=float3(0,nx.y,nx.x)*w.x+float3(ny.x,0,ny.y)*w.y+float3(nz.x,nz.y,0)*w.z;
    n=normalize(n+detail);
    float stain=.5+.5*sin(i.positionWS.x*.47+sin(i.positionWS.z*.6)+i.positionWS.y*.38);
    float damp=_Damp*stain*saturate(1-i.positionWS.y*.16);
    a=lerp(dot(a,half3(.2126,.7152,.0722)).xxx,a,.45)*_BaseColor.rgb*(1-damp*.3);
    SurfaceData s=(SurfaceData)0;s.albedo=a;s.alpha=1;s.metallic=0;s.specular=half3(.04,.04,.04);s.smoothness=clamp(1-rough+damp*.35,.04,.48);s.normalTS=half3(0,0,1);s.occlusion=1;
    InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);d.bakedGI=SampleSH(n);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);d.shadowMask=half4(1,1,1,1);d.vertexLighting=VertexLighting(i.positionWS,n);
    half4 c=UniversalFragmentPBR(d,s);c.rgb=MixFog(c.rgb,i.fog);return c;
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
  UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
}
