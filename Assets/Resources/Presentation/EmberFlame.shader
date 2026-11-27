Shader "Cinders/EmberFlame"
{
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass {
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
 struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 half4 frag(V i):SV_Target {
 float y=i.uv.y;
 float n=noise(float2(i.uv.x*5,y*5-_Time.y*3));
 float n2=noise(float2(i.uv.x*11,y*9-_Time.y*5));
 float sway=sin(y*7-_Time.y*3)*y*.12+(n-.5)*y*.2;
 float x=abs(i.uv.x-.5+sway);
 float width=.32*(1-y)+.035;
 // soft tongue: smooth falloff from the axis (the hard x12 edge drew a flat trapezoid), radial fade near the quad border
 float shape=smoothstep(0,1,saturate((width-x)/max(width,.001)*1.6+(n-.5)*.5+(n2-.5)*.25));
 float border=saturate(1-length(i.uv-.5)*1.9);
 float alpha=shape*border*smoothstep(0,.15,y)*(1-smoothstep(.55,.95,y));
 float heat=saturate(shape*(1-y));
 half3 color=lerp(half3(.75,.055,.006),half3(2.2,1.15,.2),heat);
 return half4(color,alpha*i.color.a);
 }
 ENDHLSL
 }
 }
}
