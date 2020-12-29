using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


[ExecuteAlways]
public class particlesOnSkinnedMeshTornado : MonoBehaviour
{




    
    
    public ComputeShader shader;
    public int numParticles = 1000;


    public SkinnedMeshRenderer renderer;

    public Material particleMaterial;
  
    // skinning ( duplicating the work unity does)
    public ComputeBuffer boneBuffer;
    public ComputeBuffer bindBuffer;

    public ComputeBuffer vertBuffer;
    public ComputeBuffer triBuffer;
    
    public ComputeBuffer particleBuffer;



    // Parameters for Compute Shader
    public Vector3  _Gravity;
    public float    _Dampening;
    public float    _NoiseSize;
    public float    _NoisePower;
    public float    _NormalPower;
    public float    _VelocityPower;
    public float    _DieHard;



    Vector3[] verts;
    Vector3[] normals;
    Vector2[] uvs;
    int[] tris;

    public Transform[] bones;
    public Matrix4x4[] bindPoses;


    // Start is called before the first frame update
    void OnEnable()
    {

        MakeSkinnedMeshBuffers();
        MakeParticleBuffer();
        
      
    }



    void MakeSkinnedMeshBuffers(){
        
        verts = renderer.sharedMesh.vertices;
        normals = renderer.sharedMesh.normals;
        uvs = renderer.sharedMesh.uv;
        tris = renderer.sharedMesh.triangles;

        BoneWeight[] weights = renderer.sharedMesh.boneWeights;


        /* 
        struct {
            float3 position;
            float3 normal;
            float2 uv;

            float3 bindPos;
            float3 bindNor;
            float4 boneWeights;
            float4 boneIDs;
            float2 debug;
        }
            3 + 3 + 2 + 3 + 3 + 4 + 4 + 2
            24
        */

        vertBuffer = new ComputeBuffer( verts.Length , 24 * sizeof(float));

        float[] vertBufferInfo = new float[ 24 * verts.Length ];

        int index = 0;
        for( int i = 0; i < verts.Length; i++ ){
           
            vertBufferInfo[ index ++ ] = 0;
            vertBufferInfo[ index ++ ] = 0;
            vertBufferInfo[ index ++ ] = 0;
           
            vertBufferInfo[ index ++ ] = 0;
            vertBufferInfo[ index ++ ] = 0;
            vertBufferInfo[ index ++ ] = 0;
           
            vertBufferInfo[ index ++ ] = uvs[i].x;
            vertBufferInfo[ index ++ ] = uvs[i].y;
           
            vertBufferInfo[ index ++ ] = verts[i].x;
            vertBufferInfo[ index ++ ] = verts[i].y;
            vertBufferInfo[ index ++ ] = verts[i].z;
           
            vertBufferInfo[ index ++ ] = normals[i].x;
            vertBufferInfo[ index ++ ] = normals[i].y;
            vertBufferInfo[ index ++ ] = normals[i].z;
           
            vertBufferInfo[ index ++ ] = weights[i].weight0;
            vertBufferInfo[ index ++ ] = weights[i].weight1;
            vertBufferInfo[ index ++ ] = weights[i].weight2;
            vertBufferInfo[ index ++ ] = weights[i].weight3;
           
            vertBufferInfo[ index ++ ] = weights[i].boneIndex0;
            vertBufferInfo[ index ++ ] = weights[i].boneIndex1;
            vertBufferInfo[ index ++ ] = weights[i].boneIndex2;
            vertBufferInfo[ index ++ ] = weights[i].boneIndex3;
           
            vertBufferInfo[ index ++ ] = 1;
            vertBufferInfo[ index ++ ] = .5f;
        
        }


        vertBuffer.SetData( vertBufferInfo );


        bones = renderer.bones;
        bindPoses = renderer.sharedMesh.bindposes;


        boneBuffer = new ComputeBuffer(bones.Length , sizeof(float) * 16 );
        bindBuffer = new ComputeBuffer(bones.Length , sizeof(float) * 16 );

        bindBuffer.SetData(bindPoses);
        
    }

    void MakeParticleBuffer(){


        /* 
        struct {
            float3 position;
            float3 velocity;

            float2 uv;

            float3 triWeights;
            float3 triIDs;
            
            float2 debug;
        }
            3 + 3 + 2 + 3 + 3 + 2
            16
        */



    float[] triAreas = new float[tris.Length / 3];
    float totalArea = 0;

    int tri0; int tri1; int tri2;

    for (int i = 0; i < tris.Length / 3; i++) {
    
      tri0 = i * 3;
      tri1 = tri0 + 1;
      tri2 = tri0 + 2;
     
      tri0 = tris[tri0];
      tri1 = tris[tri1];
      tri2 = tris[tri2];
     
      float area = 1;

        area = AreaOfTriangle (verts[tri0], verts[tri1], verts[tri2]);
     
      triAreas[i] = area;
      totalArea += area;
    
    }

    for (int i = 0; i < triAreas.Length; i++) {
      triAreas[i] /= totalArea;
    }

        float[] particleValues = new float[numParticles * 16];

        int index = 0;
        for( int i = 0; i < numParticles; i++ ){


 


            particleValues[index++] = 0;
            particleValues[index++] = 0;
            particleValues[index++] = 0;

            particleValues[index++] = 0;
            particleValues[index++] = 0;
            particleValues[index++] = 0;
        
            particleValues[index++] = 0;
            particleValues[index++] = 0;

            

          
           
            int baseTri = GetTri(Random.Range(0.00f,.9999f),triAreas);// * ((float)tris.Length/3));
         

         int t0 = tris[baseTri*3 + 0 ];
         int t1 = tris[baseTri*3 + 1 ];
         int t2 = tris[baseTri*3 + 2 ];

        Vector3 pos = GetRandomPointInTriangle(i, verts[t0], verts[t1], verts[t2]);

      float a0 = AreaOfTriangle(pos, verts[t1], verts[t2]);
      float a1 = AreaOfTriangle(pos, verts[t0], verts[t2]);
      float a2 = AreaOfTriangle(pos, verts[t0], verts[t1]);

      float aTotal = a0 + a1 + a2;

      float p0 = a0 / aTotal;
      float p1 = a1 / aTotal;
      float p2 = a2 / aTotal;
 
        Vector3 p = new Vector3( Random.Range(0.000f,.999f), Random.Range(0.000f,.999f), Random.Range(0.000f,.999f));
            p = p.normalized;


            particleValues[index++] = p0;
            particleValues[index++] = p1;
            particleValues[index++] = p2;
        

            particleValues[index++] = tris[baseTri*3 + 0 ];
            particleValues[index++] = tris[baseTri*3 + 1 ];
            particleValues[index++] = tris[baseTri*3 + 2 ];


        
            particleValues[index++] = 0;
            particleValues[index++] = 0;
        
        }





        particleBuffer = new ComputeBuffer( numParticles , sizeof(float)  * 16 );
        particleBuffer.SetData(particleValues);

    }

    void OnDisable(){
        if( vertBuffer != null ){ vertBuffer.Release(); }
        if( bindBuffer != null ){ bindBuffer.Release(); }
        if( boneBuffer != null ){ boneBuffer.Release(); }
        if( particleBuffer != null ){ particleBuffer.Release(); }
    }

    MaterialPropertyBlock mpb;
    // Update is called once per frame
    void LateUpdate()
    {


        if(mpb == null ){
            mpb = new MaterialPropertyBlock();
        }

        Matrix4x4[] boneInfo = new Matrix4x4[bones.Length];
        for( int i = 0; i < bones.Length; i++ ){
            boneInfo[i] = bones[i].localToWorldMatrix;
        }

        boneBuffer.SetData(boneInfo);


        uint y; uint z; uint numThreads; int numGroups;
        shader.GetKernelThreadGroupSizes(0, out numThreads , out y, out z);
        numGroups = (verts.Length+((int)numThreads-1))/(int)numThreads;
        
        shader.SetBuffer(0,"_VertBuffer", vertBuffer);
        shader.SetBuffer(0,"_BindBuffer", bindBuffer);
        shader.SetBuffer(0,"_BoneBuffer", boneBuffer);
        
        shader.Dispatch( 0,numGroups ,1,1);


  shader.SetVector("_TornadoPosition", transform.position);

        shader.SetVector(   "_Gravity"        , _Gravity       );
        shader.SetFloat(    "_Dampening"      , _Dampening     );
        shader.SetFloat(    "_NoiseSize"      , _NoiseSize     );
        shader.SetFloat(    "_NoisePower"     , _NoisePower    );
        shader.SetFloat(    "_NormalPower"    , _NormalPower   );
        shader.SetFloat(    "_VelocityPower"  , _VelocityPower );
        shader.SetFloat(    "_DieHard"        , _DieHard       );

        shader.GetKernelThreadGroupSizes(1, out numThreads , out y, out z);
        numGroups = (numParticles+((int)numThreads-1))/(int)numThreads;
        
        shader.SetBuffer(1,"_VertBuffer", vertBuffer);
        shader.SetBuffer(1,"_ParticleBuffer", particleBuffer);
        
        shader.Dispatch( 1,numGroups ,1,1);





        mpb.SetBuffer("_VertBuffer", vertBuffer);
        mpb.SetBuffer("_ParticleBuffer", particleBuffer);
        mpb.SetBuffer("_BindBuffer", bindBuffer);
        mpb.SetBuffer("_BoneBuffer", boneBuffer);
        
        Graphics.DrawProcedural( 
                particleMaterial ,  
                new Bounds(transform.position, Vector3.one * 5000), 
                MeshTopology.Triangles,
                numParticles * 3 * 2, 
                1, null, mpb, 
                ShadowCastingMode.Off, 
                true, 
                LayerMask.NameToLayer("Default")
            );
          
        
    }




     public static Vector3 GetRandomPointInTriangle( int seed, Vector3 v1 , Vector3 v2 , Vector3 v3 ){
   
    /* Triangle verts called a, b, c */

    Random.InitState(seed* 14145);
    float r1 = Random.value;

    Random.InitState(seed* 19247);
    float r2 = Random.value;
    //float r3 = Random.value;

    return (1 - Mathf.Sqrt(r1)) * v1 + (Mathf.Sqrt(r1) * (1 - r2)) * v2 + (Mathf.Sqrt(r1) * r2) * v3;
     
    ///return (r1 * v1 + r2 * v2 + r3 * v3) / (r1 + r2 + r3);
  }

  public static float AreaOfTriangle( Vector3 v1 , Vector3 v2 , Vector3 v3 ){
     Vector3 v = Vector3.Cross(v1-v2, v1-v3);
     float area = v.magnitude * 0.5f;
     return area;
  }


    public static int GetTri(float randomVal, float[] triAreas){


    int triID = 0;
    float totalTest = 0;
    for( int i = 0; i < triAreas.Length; i++ ){

      totalTest += triAreas[i];
      if( randomVal <= totalTest){
        triID = i;
        break;
      }

    }

    return triID;

  }



}
