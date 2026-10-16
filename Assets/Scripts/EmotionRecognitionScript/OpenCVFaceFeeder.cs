using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;

public class OpenCVFaceFeeder : MonoBehaviour
{
    public FERController fer;           
    public RawImage display;          
    public float readInterval = 0.2f;
    public bool useFrontCamera = true;

    WebCamTexture webcam;
    Mat camMat, rgbaMat, grayMat;
    CascadeClassifier cascade;
    Texture2D displayTex, faceTex;
    float lastRead;
    string lastLabel = "";
    bool isFrontFacing;

    void OnEnable()
    {
        string camName = null;
        isFrontFacing = false;
        foreach (WebCamDevice device in WebCamTexture.devices)
        {
            if (device.isFrontFacing == useFrontCamera)
            {
                camName = device.name;
                isFrontFacing = device.isFrontFacing;
                break;
            }
        }
        if (camName == null)
            Debug.LogWarning("Requested camera not found - using the default camera");

        webcam = camName != null ? new WebCamTexture(camName, 640, 480) : new WebCamTexture(640, 480);
        webcam.Play();

        cascade = new CascadeClassifier();
        cascade.load(Utils.getFilePath("haarcascade_frontalface_alt.xml"));
        if (cascade.empty())
            Debug.LogError("Cascade not found - put haarcascade_frontalface_alt.xml in Assets/StreamingAssets/");
    }

    void Update()
    {
        if (webcam == null || !webcam.didUpdateThisFrame || webcam.width < 100) return;

        if (camMat == null || camMat.cols() != webcam.width || camMat.rows() != webcam.height)
        {
            camMat?.Dispose();
            camMat = new Mat(webcam.height, webcam.width, CvType.CV_8UC4);
        }
        if (rgbaMat == null) { rgbaMat = new Mat(); grayMat = new Mat(); }

        // flip=true converts Unity's bottom-up pixels to a top-down Mat; skip it if the camera is already vertically mirrored
        Utils.webCamTextureToMat(webcam, camMat, !webcam.videoVerticallyMirrored);
        RotateUpright(camMat, rgbaMat, webcam.videoRotationAngle);
        if (isFrontFacing) Core.flip(rgbaMat, rgbaMat, 1); // mirror like a selfie

        Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
        Imgproc.equalizeHist(grayMat, grayMat);
        MatOfRect facesRes = new MatOfRect();
        cascade.detectMultiScale(grayMat, facesRes, 1.2, 5, 0, new Size(80, 80), new Size());
        OpenCVForUnity.CoreModule.Rect[] faces = facesRes.toArray();

        if (faces.Length > 0)
        {

            OpenCVForUnity.CoreModule.Rect r = faces[0];
            foreach (var f in faces) if (f.width * f.height > r.width * r.height) r = f;


            if (Time.time - lastRead >= readInterval && fer != null)
            {
                lastRead = Time.time;
                using (Mat faceMat = new Mat(rgbaMat, r))
                {
                    if (faceTex == null || faceTex.width != r.width || faceTex.height != r.height)
                        faceTex = new Texture2D(r.width, r.height, TextureFormat.RGBA32, false);
                    Utils.matToTexture2D(faceMat, faceTex);
                    var (emotion, conf) = fer.PredictEmotion(faceTex);
                    lastLabel = $"{emotion} {conf * 100:F0}%";
                }
            }

            Imgproc.rectangle(rgbaMat, new Point(r.x, r.y), new Point(r.x + r.width, r.y + r.height),
                              new Scalar(0, 200, 0, 255), 2);
            Imgproc.putText(rgbaMat, lastLabel, new Point(r.x, r.y - 10),
                            Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar(0, 200, 0, 255), 2);
        }
        else lastLabel = "";
        
        if (display != null)
        {
            if (displayTex == null || displayTex.width != rgbaMat.cols() || displayTex.height != rgbaMat.rows())
            {
                if (displayTex != null) Destroy(displayTex);
                displayTex = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            }
            Utils.matToTexture2D(rgbaMat, displayTex);
            display.texture = displayTex;
            FitToDisplay();
        }
    }

    void OnDisable()
    {
        if (webcam != null)
        {
            webcam.Stop();
            Destroy(webcam);
            webcam = null;
        }

        camMat?.Dispose();
        camMat = null;
        rgbaMat?.Dispose();
        rgbaMat = null;
        grayMat?.Dispose();
        grayMat = null;

        cascade?.Dispose();
        cascade = null;

        if (faceTex != null) Destroy(faceTex);
        if (displayTex != null) Destroy(displayTex);
        faceTex = null;
        displayTex = null;
        lastLabel = "";
    }

    // Crop the camera image to the RawImage's shape so it isn't stretched or squashed
    void FitToDisplay()
    {
        Vector2 size = display.rectTransform.rect.size;
        if (size.x <= 0 || size.y <= 0) return;

        float boxAspect = size.x / size.y;
        float texAspect = (float)displayTex.width / displayTex.height;
        if (texAspect > boxAspect)
        {
            float w = boxAspect / texAspect;
            display.uvRect = new UnityEngine.Rect((1 - w) / 2, 0, w, 1);
        }
        else
        {
            float h = texAspect / boxAspect;
            display.uvRect = new UnityEngine.Rect(0, (1 - h) / 2, 1, h);
        }
    }

    // Android cameras deliver frames rotated; turn them upright so the face cascade can detect faces
    static void RotateUpright(Mat src, Mat dst, int angle)
    {
        switch (angle)
        {
            case 90:  Core.rotate(src, dst, Core.ROTATE_90_CLOCKWISE); break;
            case 180: Core.rotate(src, dst, Core.ROTATE_180); break;
            case 270: Core.rotate(src, dst, Core.ROTATE_90_COUNTERCLOCKWISE); break;
            default:  src.copyTo(dst); break;
        }
    }
}
