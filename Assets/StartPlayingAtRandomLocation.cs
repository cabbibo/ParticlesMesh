using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartPlayingAtRandomLocation : MonoBehaviour
{


    public AudioSource audio;
    // Start is called before the first frame update
    void Start()
    {
        
        audio.time = Random.Range( 0, audio.clip.length );
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
