Shader "Randint/TerminalHologram"
{
 Properties { [MainColor] _BaseColor("Theme colour", Color) = (1,1,1,1) }
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
   struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
   struct V { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; };
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   CBUFFER_END
   V vert(A i) { V o; o.positionWS=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS); o.normalWS=TransformObjectToWorldNormal(i.normalOS); return o; }
   half4 frag(V i):SV_Target {
    half rim=pow(1-saturate(abs(dot(normalize(i.normalWS),normalize(GetWorldSpaceViewDir(i.positionWS))))),2);
    half scan=.94+.06*sin(i.positionWS.y*90);
    return half4(_BaseColor.rgb*(.45+rim*1.5)*scan, .16+rim*.7);
   }
   ENDHLSL
  }
 }
}
