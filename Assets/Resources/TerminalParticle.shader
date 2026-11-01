Shader "Randint/TerminalParticle"
{
 Properties { _BaseColor("Theme multiplier", Color) = (1,1,1,1) }
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
   struct A { float4 positionOS:POSITION; half4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; half4 color:COLOR; };
   half4 _BaseColor;
   V vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.color=i.color*_BaseColor; return o; }
   half4 frag(V i):SV_Target { return i.color; }
   ENDHLSL
  }
 }
}
