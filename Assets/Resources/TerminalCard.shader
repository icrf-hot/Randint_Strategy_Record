Shader "Randint/TerminalCard"
{
 Properties {
  [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
  _PanelColor("Theme panel", Color) = (1,1,1,1)
  _InkColor("Theme ink", Color) = (1,1,1,1)
 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
  Pass {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   half4 _PanelColor, _InkColor;
   V vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; o.color=i.color; return o; }
   half4 frag(V i):SV_Target { half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv); half luminance=dot(c.rgb,half3(.2126,.7152,.0722)); return half4(lerp(_InkColor.rgb,_PanelColor.rgb,luminance),c.a*i.color.a); }
   ENDHLSL
  }
 }
}
