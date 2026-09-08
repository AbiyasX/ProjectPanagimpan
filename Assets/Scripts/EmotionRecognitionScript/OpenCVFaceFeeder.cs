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

    WebCamTexture webcam;
    Mat rgbaMat, grayMat;
    CascadeClassifier cascade;
    Texture2D displayTex, faceTex;
    float lastRead;
    string lastLabel = "";

    void OnEnable()
    {
        webcam = new WebCamTexture(640, 480);
        webcam.Play();

        cascade = new CascadeClassifier();
        cascade.load(Utils.getFilePath("haarcascade_frontalface_alt.xml"));
        if (cascade.empty())
            Debug.LogError("Cascade not found - put haarcascade_frontalface_alt.xml in Assets/StreamingAssets/");
    }

    void Update()
    {
        if (webcam == null || !webcam.didUpdateThisFrame || webcam.width < 100) return;

        if (rgbaMat == null) { rgbaMat = new Mat(webcam.height, webcam.width, CvType.CV_8UC4); grayMat = new Mat(); }
        Utils.webCamTextureToMat(webcam, rgbaMat);

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
            if (displayTex == null) displayTex = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(rgbaMat, displayTex);
            display.texture = displayTex;
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
}
