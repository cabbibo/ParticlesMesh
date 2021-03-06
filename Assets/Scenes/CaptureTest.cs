//C# script example
using UnityEngine;
using System.Collections;

public class CaptureTest : MonoBehaviour {
    // Capture frames as a screenshot sequence. Images are
    // stored as PNG files in a folder - these can be combined into
    // a movie using image utility software (eg, QuickTime Pro).
    // The folder to contain our screenshots.
    // If the folder exists we will append numbers to create an empty folder.
    string folder = "ScreenshotFolder";
    public int frameRate = 60;
    public int superSize;
    public string folderName;

    public int startFrameCount;

    public bool capturing;
    private bool oCapturing;

    private string final;

    public int currentFrame;
    public float currentTime;

    void Start () {


        final = folder;

        // Create the folder
        System.IO.Directory.CreateDirectory(final);


    }

    void Update () {

        if( capturing == true && oCapturing == false ){
            Time.captureFramerate = frameRate;
            startFrameCount = Time.frameCount;
        }

         if( capturing == false && oCapturing == true ){
            Time.captureFramerate = 0;
        }

        if( capturing == true ){


            currentFrame = Time.frameCount - startFrameCount;
            currentTime = (float)currentFrame / (float)frameRate;

            // Append filename to folder name (format is '0005 shot.png"')
            string name = string.Format("{0}/shot{1:D04}.png", final, currentFrame );

            // Capture the screenshot to the specified file.
            ScreenCapture.CaptureScreenshot(name,superSize);

        }
        oCapturing = capturing;
    }


}
