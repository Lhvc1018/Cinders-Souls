Shader "Cinders/CemeterySky" {
 Properties { _Horizon("Horizon", Color) = (.18,.23,.27,1) _Zenith("Zenith", Color) = (.035,.055,.09,1) }
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f {float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
 float4 _Horizon,_Zenith;
 v2f vert(float4 v:POSITION){v2f o;o.pos=UnityObjectToClipPos(v);o.dir=v.xyz;return o;}
 half4 frag(v2f i):SV_Target {float h=saturate(normalize(i.dir).y);return lerp(_Horizon,_Zenith,smoothstep(0,.85,h));}
 ENDCG }
 } Fallback Off
}
