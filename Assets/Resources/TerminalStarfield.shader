Shader "Randint/TerminalStarfield"
{
 Properties {
  _BaseColor("Theme background", Color) = (0,0,0,1)
  _StarColor("Theme stars", Color) = (1,1,1,1)
  _AccentColor("Theme accent", Color) = (1,1,1,1)
  _MotionTime("Twinkle time", Float) = 0
 }
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
  Cull Off ZWrite Off
  Pass {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A { float4 positionOS:POSITION; };
   struct V { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
   half4 _BaseColor, _StarColor, _AccentColor;
   float _MotionTime;
   float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   V vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.direction=i.positionOS.xyz; return o; }
   half4 frag(V i):SV_Target {
    float3 d=normalize(i.direction);
    float2 uv=float2(atan2(d.x,d.z)/6.2831853+.5,asin(clamp(d.y,-1,1))/3.14159265+.5);
    float2 p=uv*float2(360,180), cell=floor(p);
    float seed=hash(cell), exists=step(.965,seed);
    float2 center=.15+.7*float2(hash(cell+13),hash(cell+47));
    float distance=length(frac(p)-center);
    float radius=.045+.045*hash(cell+7);
    // Differentiate continuous coordinates; frac-cell edges must not create streaks.
    float2 pixelSpan=fwidth(p);
    float aa=clamp(max(pixelSpan.x,pixelSpan.y)*.5,.006,.04);
    float star=(1-smoothstep(radius-aa,radius+aa,distance))*exists;
    float twinkle=.55+.45*sin(_MotionTime*(.5+hash(cell+91)) + seed*73);
    half3 ink=lerp(_StarColor.rgb,_AccentColor.rgb,step(.82,hash(cell+29)));
    return half4(lerp(_BaseColor.rgb,ink,star*(.28+.55*twinkle)),1);
   }
   ENDHLSL
  }
 }
}
