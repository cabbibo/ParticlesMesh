Shader "Unlit/debugVertShader"
{
    Properties
    {
        _Size ("Size", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct Vert{
                float3 position;
                float3 normal;
                float2 uv;

                float3 bindPos;
                float3 bindNor;
                float4 boneWeights;
                float4 boneIDs;
                float2 debug;
            };

            StructuredBuffer<Vert> _VertBuffer;




            StructuredBuffer<float4x4> _BoneBuffer;
            StructuredBuffer<float4x4> _BindBuffer;

          

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 debug : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            float _Size;

            v2f vert (uint vid : SV_VertexID)
            {
                v2f o;

                int vertID = vid/6;
                int alternate = vid % 6;


                float3 left = normalize( UNITY_MATRIX_VP[0].xyz);
                float3 up = normalize( UNITY_MATRIX_VP[1].xyz);


                Vert v = _VertBuffer[vertID];



  
  float4x4 bi0 = _BindBuffer[ int(v.boneIDs[0]) ];
  float4x4 bi1 = _BindBuffer[ int(v.boneIDs[1]) ];
  float4x4 bi2 = _BindBuffer[ int(v.boneIDs[2]) ];
  float4x4 bi3 = _BindBuffer[ int(v.boneIDs[3]) ];


  
  float4x4 bo0 = _BoneBuffer[ int(v.boneIDs[0]) ];
  float4x4 bo1 = _BoneBuffer[ int(v.boneIDs[1]) ];
  float4x4 bo2 = _BoneBuffer[ int(v.boneIDs[2]) ];
  float4x4 bo3 = _BoneBuffer[ int(v.boneIDs[3]) ];
                
  float4x4 m0 = mul(bo0,bi0);
  float4x4 m1 = mul(bo1,bi1);
  float4x4 m2 = mul(bo2,bi2);
  float4x4 m3 = mul(bo3,bi3);



  //float3 localVertPositionRelatedToBone0 = mul( bi0 , float4( v.bindPos , 1 ) ).xyz;
  //float3 worldVertPositionUsingBone0Transform = mul( bo0 , float4(localVertPositionRelatedToBone0,1) ).xyz;
  
  
  float3 bp0 = mul( m0 , float4( v.bindPos , 1 ) ).xyz;
  float3 bp1 = mul( m1 , float4( v.bindPos , 1 ) ).xyz;
  float3 bp2 = mul( m2 , float4( v.bindPos , 1 ) ).xyz;
  float3 bp3 = mul( m3 , float4( v.bindPos , 1 ) ).xyz;



  
  //float3 bp0 = mul( m0 , float4( v.bindPos , 1 ) ).xyz;
  //float3 bp1 = mul( m1 , float4( v.bindPos , 1 ) ).xyz;
  //float3 bp2 = mul( m2 , float4( v.bindPos , 1 ) ).xyz;
  //float3 bp3 = mul( m3 , float4( v.bindPos , 1 ) ).xyz;

  float3 fBindPos  = bp0 * v.boneWeights[0];
         fBindPos += bp1 * v.boneWeights[1];
         fBindPos += bp2 * v.boneWeights[2];
         fBindPos += bp3 * v.boneWeights[3];



                float3 p1 = v.position + ( - left - up ) * _Size;
                float3 p2 = v.position + ( + left - up ) * _Size;
                float3 p3 = v.position + ( - left + up ) * _Size;
                float3 p4 = v.position + ( + left + up ) * _Size;


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

                o.debug = v.debug;


                o.vertex = mul(UNITY_MATRIX_VP, float4(fPos,1.0f));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = float4(0,0,0,1);// tex2D(_MainTex, i.uv);
                col.r = i.debug.x;
                col.g = i.debug.y;
             
                return col;
            }
            ENDCG
        }
    }
}
