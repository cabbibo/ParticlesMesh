Shader "ParticlesOnSkinnedMesh/ShadowVelocityParticles"
{
    Properties
    {
        _Size ("Size", Float) = 0.1
        _AlphaCuttoff ("AlphaCuttoff", Float) = 0.1
        _SpriteSize("SpriteSize",int) = 6
        _MainTex ("tex" , 2D )  = "white" {}
        _StartingColor("_Color", Color ) = (1,1,1,1)
        _EndingColor("_Color", Color ) = (1,1,1,1)
    }
    SubShader
    {
        
        Tags{ "LightMode" = "ForwardBase" }
        LOD 100

        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #pragma target 4.5
 #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

      #include "UnityCG.cginc"
      #include "AutoLight.cginc"


        struct Particle {
            float3 pos;
            float3 vel;

            float2 uv;

            float3 triWeights;
            float3 triIDs;
            
            float2 debug;
        };


    StructuredBuffer<Particle> _ParticleBuffer;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD4;
                float2 debug : TEXCOORD1;
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD3;
                float id : TEXCOORD2;
                float3 vel : TEXCOORD7;
                
                LIGHTING_COORDS(5,6)
            };

                  float hash( float n ){
        return frac(sin(n)*43758.5453);
      }

            float _Size;
            float _AlphaCuttoff;
            int _SpriteSize;
            sampler2D _MainTex;

            v2f vert (uint vid : SV_VertexID)
            {
                v2f o;

                int vertID = vid/6;
                int alternate = vid % 6;


                float3 left = normalize( UNITY_MATRIX_VP[0].xyz);
                float3 up = normalize( UNITY_MATRIX_VP[1].xyz);

                Particle v = _ParticleBuffer[vertID];

                float3 p1 = v.pos + ( - left - up ) * _Size * min( (1-v.debug.x) * 5 ,  v.debug.x);
                float3 p2 = v.pos + ( + left - up ) * _Size * min( (1-v.debug.x) * 5 ,  v.debug.x);
                float3 p3 = v.pos + ( - left + up ) * _Size * min( (1-v.debug.x) * 5 ,  v.debug.x);
                float3 p4 = v.pos + ( + left + up ) * _Size * min( (1-v.debug.x) * 5 ,  v.debug.x);

                float3 fPos;
                float2 fUV;

                 if( alternate == 0 ){
                    fPos = p1;
                    fUV = float2(0,0);
                }else if( alternate == 1){
                    fPos = p2;
                    fUV = float2(1,0);
                }else if( alternate == 2){
                    fPos = p4;
                    fUV = float2(1,1);
                }else if( alternate == 3){
                    fPos = p1;
                    fUV = float2(0,0);
                }else if( alternate == 4){
                    fPos = p4;
                    fUV = float2(1,1);
                }else{
                    fPos = p3;
                    fUV = float2(0,1);
                }


                float col = hash( float(vertID * 10));
                float row = hash( float(vertID * 20));

                o.uv2 = (fUV + floor(_SpriteSize * float2( col , row )))/_SpriteSize;

        o.vel = v.vel;
                o.debug = v.debug;
                o.uv = fUV;
                o.id = float(vertID);
                o.world = fPos;

                o.pos = mul(UNITY_MATRIX_VP, float4(fPos,1.0f));

                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            
            }

            float4 _StartingColor;
            float4 _EndingColor;
            fixed4 frag (v2f i) : SV_Target
            {

                float atten = LIGHT_ATTENUATION(i);
                // sample the texture
                fixed4 col = tex2D(_MainTex , i.uv2);
                if(col.a < _AlphaCuttoff){discard;}

                //col *= lerp(_StartingColor , _EndingColor , i.debug.x);
                col.xyz = normalize(i.vel) * .5 + .5;
                col.xyz *= length(i.vel) * 20;
                col *= atten * .8 + .2;


             
                return col;
            }
            ENDCG
        }
 













































    
         Pass
    {
      Tags{ "LightMode" = "ShadowCaster" }


      Fog{ Mode Off }
      ZWrite On
      ZTest LEqual
      Cull Off
      Offset 1, 1
      CGPROGRAM

      #pragma target 4.5
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_shadowcaster
      #pragma fragmentoption ARB_precision_hint_fastest

      #include "UnityCG.cginc"


        struct Particle {
            float3 pos;
            float3 vel;

            float2 uv;

            float3 triWeights;
            float3 triIDs;
            
            float2 debug;
        };


    StructuredBuffer<Particle> _ParticleBuffer;

            float _Size;
            float _AlphaCuttoff;
            int _SpriteSize;
            sampler2D _MainTex;

      struct v2f {
        V2F_SHADOW_CASTER;
        float2 uv : TEXCOORD1;
        float2 uv2 : TEXCOORD2;
        float3 nor : NORMAL;
      };



      float hash( float n ){
        return frac(sin(n)*43758.5453);
      }




float4 ShadowCasterPos (float3 vertex, float3 normal) {
  float4 clipPos;
    
    // Important to match MVP transform precision exactly while rendering
    // into the depth texture, so branch on normal bias being zero.
    if (unity_LightShadowBias.z != 0.0) {
    float3 wPos = vertex.xyz;
    float3 wNormal = normal;
    float3 wLight = normalize(UnityWorldSpaceLightDir(wPos));

  // apply normal offset bias (inset position along the normal)
  // bias needs to be scaled by sine between normal and light direction
  // (http://the-witness.net/news/2013/09/shadow-mapping-summary-part-1/)
  //
  // unity_LightShadowBias.z contains user-specified normal offset amount
  // scaled by world space texel size.

    float shadowCos = dot(wNormal, wLight);
    float shadowSine = sqrt(1 - shadowCos * shadowCos);
    float normalBias = unity_LightShadowBias.z * shadowSine;

    wPos -= wNormal * normalBias;
    clipPos = mul(UNITY_MATRIX_VP, float4(wPos, 1));
    }
    else {
        clipPos = UnityObjectToClipPos(vertex);
    }
  return clipPos;
}

float4x4 _InverseWorldMatrix;

      v2f vert(appdata_base input, uint vid : SV_VertexID)
      {
        v2f o;


       



 int vertID = vid/6;
                int alternate = vid % 6;


                float3 left = normalize( UNITY_MATRIX_VP[0].xyz);
                float3 up = normalize( UNITY_MATRIX_VP[1].xyz);

                Particle v = _ParticleBuffer[vertID];

                float3 p1 = v.pos + ( - left - up ) * _Size * min( (1-v.debug.x) * 5 ,  v.debug.x);
                float3 p2 = v.pos + ( + left - up ) * _Size * min( (1-v.debug.x) * 5 ,  v.debug.x);
                float3 p3 = v.pos + ( - left + up ) * _Size * min( (1-v.debug.x) * 5 ,  v.debug.x);
                float3 p4 = v.pos + ( + left + up ) * _Size * min( (1-v.debug.x) * 5 ,  v.debug.x);

                float3 fPos;
                float2 fUV;

                 if( alternate == 0 ){
                    fPos = p1;
                    fUV = float2(0,0);
                }else if( alternate == 1){
                    fPos = p2;
                    fUV = float2(1,0);
                }else if( alternate == 2){
                    fPos = p4;
                    fUV = float2(1,1);
                }else if( alternate == 3){
                    fPos = p1;
                    fUV = float2(0,0);
                }else if( alternate == 4){
                    fPos = p4;
                    fUV = float2(1,1);
                }else{
                    fPos = p3;
                    fUV = float2(0,1);
                }


                float col = hash( float(vertID * 10));
                float row = hash( float(vertID * 20));

                o.uv2 = (fUV + floor(_SpriteSize * float2( col , row )))/_SpriteSize;


               // o.debug = v.debug;
                o.uv = fUV;
               // o.id = float(vertID);
                //o.world = fPos;

 // fPos =  mul(_InverseWorldMatrix, float4(fPos,1)).xyz;
        float4 position = ShadowCasterPos(fPos,0);
        o.pos = UnityApplyLinearShadowBias(position);
        return o;
      }

      float4 frag(v2f v) : COLOR
      {           float4 spritCol = tex2D(_MainTex,v.uv2);
               if( spritCol.a < _AlphaCuttoff){discard;}
                
        SHADOW_CASTER_FRAGMENT(v)
      }
      ENDCG
    }


   }


}
