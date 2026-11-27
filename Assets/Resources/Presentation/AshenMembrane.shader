Shader "Cinders/AshenMembrane"
{
 Properties { _BaseColor("Mist",Color)=(.18,.4,.48,1) }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass {
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
 struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor;
 CBUFFER_END
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
 half4 frag(V i):SV_Target {
 float2 p=i.uv*2-1;float d=length(p);float a=atan2(p.y,p.x);
 float mist=noise(p*5+float2(_Time.y*.12,-_Time.y*.18));
 float flow=sin(d*16-a*3-_Time.y*.7+mist*4)*.5+.5;
 float veins=pow(saturate(sin(a*7+d*13+_Time.y*.5+mist*5)),12)*mist;
 float rim=smoothstep(.7,.95,d)*(1-smoothstep(.95,1,d));
 float fade=1-smoothstep(.88,1,d);
 return half4(_BaseColor.rgb*(.12+flow*.3+veins*.45)+rim*.10,fade*(.75+mist*.15));
 }
 ENDHLSL
 }
 }
}
