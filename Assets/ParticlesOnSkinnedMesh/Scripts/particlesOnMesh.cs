using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


[ExecuteAlways]
public class particlesOnMesh : MonoBehaviour
{



    // how many particles we want!
    public int numParticles = 1000;
    
    // This is what does our physics 
    // and our skinning
    public ComputeShader shader;


    // this is what we get all our information from!
    public SkinnedMeshRenderer renderer;

    // this is what does our particle rendering
    public Material particleMaterial;

    
    // Parameters for Compute Shader
    public Vector3  _Gravity;
    public float    _Dampening;
    public float    _NoiseSize;
    public float    _NoisePower;
    public float    _NormalPower;
    public float    _VelocityPower;
    public float    _DieHard;



    // All our buffers
    ComputeBuffer boneBuffer; // for current bones ( update every frame )
    ComputeBuffer bindBuffer; // for bind transforms ( update once )
    ComputeBuffer vertBuffer; // for our mesh verts ( calculate every frame )
    ComputeBuffer particleBuffer; // for our final particle sim ( calc every frame )







    // Start is called before the first frame update
    void OnEnable()
    {

        MakeSkinnedMeshBuffers();
        MakeParticleBuffer();
        
      
    }




    /*

 _____ _    _                      _  ___  ___          _     
/  ___| |  (_)                    | | |  \/  |         | |    
\ `--.| | ___ _ __  _ __   ___  __| | | .  . | ___  ___| |__  
 `--. \ |/ / | '_ \| '_ \ / _ \/ _` | | |\/| |/ _ \/ __| '_ \ 
/\__/ /   <| | | | | | | |  __/ (_| | | |  | |  __/\__ \ | | |
\____/|_|\_\_|_| |_|_| |_|\___|\__,_| \_|  |_/\___||___/_| |_|
                                                              
                                                              
In this section we are duplicating the data for a unity skinned mesh
into a compute buffer so we can do the mesh skinning ourselves

    */


    Vector3[] verts;
    Vector3[] normals;
    Vector2[] uvs;
    int[] tris;

    Transform[] bones;
    Matrix4x4[] bindPoses;

    void MakeSkinnedMeshBuffers(){
        

        // getting data from mesh all at once
        // so we aren't rereferencing
        verts = renderer.sharedMesh.vertices;
        normals = renderer.sharedMesh.normals;
        uvs = renderer.sharedMesh.uv;
        tris = renderer.sharedMesh.triangles;

        BoneWeight[] weights = renderer.sharedMesh.boneWeights;


        /* 

          This is what our struct will look like in the GPU,
          duplicating here to get the proper count.

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


        // making the actual buffer
        vertBuffer = new ComputeBuffer( verts.Length , 24 * sizeof(float));

        // creating a float array to populate with the data
        float[] vertBufferInfo = new float[ 24 * verts.Length ];

        int index = 0;
        for( int i = 0; i < verts.Length; i++ ){
           

            // this will be our final position
            // which we will calculate and dont
            // need to assign now
            vertBufferInfo[ index ++ ] = 0;
            vertBufferInfo[ index ++ ] = 0;
            vertBufferInfo[ index ++ ] = 0;
           
            // final normal ( don't need to assign now)
            vertBufferInfo[ index ++ ] = 0;
            vertBufferInfo[ index ++ ] = 0;
            vertBufferInfo[ index ++ ] = 0;
           

            // uv ( won't change )
            vertBufferInfo[ index ++ ] = uvs[i].x;
            vertBufferInfo[ index ++ ] = uvs[i].y;
           
            // bound position
            vertBufferInfo[ index ++ ] = verts[i].x;
            vertBufferInfo[ index ++ ] = verts[i].y;
            vertBufferInfo[ index ++ ] = verts[i].z;
           

            // bound normal
            vertBufferInfo[ index ++ ] = normals[i].x;
            vertBufferInfo[ index ++ ] = normals[i].y;
            vertBufferInfo[ index ++ ] = normals[i].z;
           

            // the weights of the different bones
            vertBufferInfo[ index ++ ] = weights[i].weight0;
            vertBufferInfo[ index ++ ] = weights[i].weight1;
            vertBufferInfo[ index ++ ] = weights[i].weight2;
            vertBufferInfo[ index ++ ] = weights[i].weight3;
           

            // the indices of the different bones
            vertBufferInfo[ index ++ ] = weights[i].boneIndex0;
            vertBufferInfo[ index ++ ] = weights[i].boneIndex1;
            vertBufferInfo[ index ++ ] = weights[i].boneIndex2;
            vertBufferInfo[ index ++ ] = weights[i].boneIndex3;
           

            // debug information
            vertBufferInfo[ index ++ ] = 1;
            vertBufferInfo[ index ++ ] = .5f;
        
        }

        // take all that data and jam it into
        // the compute buffer
        vertBuffer.SetData( vertBufferInfo );


        bones = renderer.bones;
        bindPoses = renderer.sharedMesh.bindposes;

        // here we are setting up the information for
        // the bone buffer ( of our current bone transforms )
        // and the original bound transforms
        boneBuffer = new ComputeBuffer(bones.Length , sizeof(float) * 16 );
        bindBuffer = new ComputeBuffer(bones.Length , sizeof(float) * 16 );

        bindBuffer.SetData(bindPoses);
        
    }




/*


______          _   _      _           
| ___ \        | | (_)    | |          
| |_/ /_ _ _ __| |_ _  ___| | ___  ___ 
|  __/ _` | '__| __| |/ __| |/ _ \/ __|
| | | (_| | |  | |_| | (__| |  __/\__ \
\_|  \__,_|_|   \__|_|\___|_|\___||___/
                                       
Here we set up our particles for use in the simulation
we need to place each particle somewhere on the triangles
of the mesh!                            



*/
    void MakeParticleBuffer(){


        /* 

        Struct for our particle in the GPU
        duplicated here to get the right count

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





    /*

      First off create a array that holds the areas of all the 
      triangles in the mesh. We can skew this if we want to 
      distribute the points differently

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
     
      float area = AreaOfTriangle (verts[tri0], verts[tri1], verts[tri2]);
     
      triAreas[i] = area;
      totalArea += area;
    
    }

    
    //Normalize the areas

    for (int i = 0; i < triAreas.Length; i++) {
      triAreas[i] /= totalArea;
    }

        float[] particleValues = new float[numParticles * 16];

        int index = 0;
        for( int i = 0; i < numParticles; i++ ){


            // position to be calucated
            particleValues[index++] = 0;
            particleValues[index++] = 0;
            particleValues[index++] = 0;

            // normal to be calculated
            particleValues[index++] = 0;
            particleValues[index++] = 0;
            particleValues[index++] = 0;
        

            // uv to be calculated
            particleValues[index++] = 0;
            particleValues[index++] = 0;

            

          
            // Get a random triangle based on the area
            int baseTri = GetTri(Random.Range(0.00f,.9999f),triAreas);// * ((float)tris.Length/3));
        

            //Get the ids for those triangle
            int t0 = tris[baseTri*3 + 0 ];
            int t1 = tris[baseTri*3 + 1 ];
            int t2 = tris[baseTri*3 + 2 ];

            // get a random position in the triangle
            Vector3 pos = GetRandomPointInTriangle(i, verts[t0], verts[t1], verts[t2]);


            // calculate values based on barycentric coordinates
            float a0 = AreaOfTriangle(pos, verts[t1], verts[t2]);
            float a1 = AreaOfTriangle(pos, verts[t0], verts[t2]);
            float a2 = AreaOfTriangle(pos, verts[t0], verts[t1]);

            float aTotal = a0 + a1 + a2;

            // getting the 'weights' for each vert
            float p0 = a0 / aTotal;
            float p1 = a1 / aTotal;
            float p2 = a2 / aTotal;
 
            Vector3 p = new Vector3( Random.Range(0.000f,.999f), Random.Range(0.000f,.999f), Random.Range(0.000f,.999f));
            p = p.normalized;

            // settting tri weights
            particleValues[index++] = p0;
            particleValues[index++] = p1;
            particleValues[index++] = p2;
        
            // setting tri ids
            particleValues[index++] = t0;
            particleValues[index++] = t1;
            particleValues[index++] = t2;


            // debug
            particleValues[index++] = 0;
            particleValues[index++] = 0;
        
        }



    
        /// assigning to our buffer
        particleBuffer = new ComputeBuffer( numParticles , sizeof(float)  * 16 );
        particleBuffer.SetData(particleValues);

    }


    // We need to release all of our buffers
    // So we don't have a memory leak
    void OnDisable(){
        if( vertBuffer != null ){ vertBuffer.Release(); }
        if( bindBuffer != null ){ bindBuffer.Release(); }
        if( boneBuffer != null ){ boneBuffer.Release(); }
        if( particleBuffer != null ){ particleBuffer.Release(); }
    }





/*



 _   _           _       _       
| | | |         | |     | |      
| | | |_ __   __| | __ _| |_ ___ 
| | | | '_ \ / _` |/ _` | __/ _ \
| |_| | |_) | (_| | (_| | ||  __/
 \___/| .__/ \__,_|\__,_|\__\___|
      | |                        
      |_|                        


*/
    MaterialPropertyBlock mpb;
    // Update is called once per frame
    void LateUpdate()
    {


        // FIRST 
        // assign all our data from our current bone transforms
        // and push it into a compute buffer
        Matrix4x4[] boneInfo = new Matrix4x4[bones.Length];
        for( int i = 0; i < bones.Length; i++ ){
            boneInfo[i] = bones[i].localToWorldMatrix;
        }

        boneBuffer.SetData(boneInfo);


        uint y; uint z; uint numThreads; int numGroups;


        /*
          
          Skinning Shader

        */
        
        // Figure out how many times we need to dispatch
        shader.GetKernelThreadGroupSizes(0, out numThreads , out y, out z);
        numGroups = (verts.Length+((int)numThreads-1))/(int)numThreads;
        
        // bind buffers
        shader.SetBuffer(0,"_VertBuffer", vertBuffer);
        shader.SetBuffer(0,"_BindBuffer", bindBuffer);
        shader.SetBuffer(0,"_BoneBuffer", boneBuffer);
        
        // Dispatch the shader 
        shader.Dispatch( 0,numGroups ,1,1);



        /*
          
          Simulation Shader

        */

        shader.SetVector(   "_Gravity"        , _Gravity       );
        shader.SetFloat(    "_Dampening"      , _Dampening     );
        shader.SetFloat(    "_NoiseSize"      , _NoiseSize     );
        shader.SetFloat(    "_NoisePower"     , _NoisePower    );
        shader.SetFloat(    "_NormalPower"    , _NormalPower   );
        shader.SetFloat(    "_VelocityPower"  , _VelocityPower );
        shader.SetFloat(    "_DieHard"        , _DieHard       );

        // Figure out how many times we need to dispatch
        shader.GetKernelThreadGroupSizes(1, out numThreads , out y, out z);
        numGroups = (numParticles+((int)numThreads-1))/(int)numThreads;
        

         // bind buffers
        shader.SetBuffer(1,"_VertBuffer", vertBuffer);
        shader.SetBuffer(1,"_ParticleBuffer", particleBuffer);
        
        
        // Dispatch the shader 
        shader.Dispatch( 1,numGroups ,1,1);



        /*

          Rendering

        */

        if(mpb == null ){
            mpb = new MaterialPropertyBlock();
        }

        mpb.SetBuffer("_VertBuffer", vertBuffer);
        mpb.SetBuffer("_ParticleBuffer", particleBuffer);
        mpb.SetBuffer("_BindBuffer", bindBuffer);
        mpb.SetBuffer("_BoneBuffer", boneBuffer);
        mpb.SetMatrix("_InverseWorldMatrix", transform.worldToLocalMatrix);
        
        Graphics.DrawProcedural( 
            particleMaterial ,  
            new Bounds(transform.position, Vector3.one * 5000), 
            MeshTopology.Triangles,
            numParticles * 3 * 2, 
            1, null, mpb, 
            ShadowCastingMode.On, 
            true, 
            LayerMask.NameToLayer("Default")
        );
          
        
    }


/*


 _   _      _                   ______                _   _                 
| | | |    | |                  |  ___|              | | (_)                
| |_| | ___| |_ __   ___ _ __   | |_ _   _ _ __   ___| |_ _  ___  _ __  ___ 
|  _  |/ _ \ | '_ \ / _ \ '__|  |  _| | | | '_ \ / __| __| |/ _ \| '_ \/ __|
| | | |  __/ | |_) |  __/ |     | | | |_| | | | | (__| |_| | (_) | | | \__ \
\_| |_/\___|_| .__/ \___|_|     \_|  \__,_|_| |_|\___|\__|_|\___/|_| |_|___/
             | |                                                           
             |_|                                                           


*/

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
