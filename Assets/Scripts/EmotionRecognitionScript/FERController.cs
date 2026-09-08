using System.Collections.Generic;
using UnityEngine;


public class FERController : MonoBehaviour
{
    public Emotions CurrentEmotion;

    [Tooltip("Assign enet_b0_8_best_vgaf.onnx here")]
    public Unity.InferenceEngine.ModelAsset modelAsset;

    [Tooltip("Average over this many recent calls (1 = off). Stabilizes the reading.")]
    public int smoothingFrames = 8;

    // Wag i reorder
    static readonly string[] EMOTIONS =
        { "Anger", "Contempt", "Disgust", "Fear", "Happiness", "Neutral", "Sadness", "Surprise" };

    const int SIZE = 224;
    static readonly float[] MEAN = { 0.485f, 0.456f, 0.406f };   
    static readonly float[] STD  = { 0.229f, 0.224f, 0.225f };

    Unity.InferenceEngine.Model model;
    Unity.InferenceEngine.Worker worker;
    RenderTexture rt;
    readonly Queue<float[]> history = new Queue<float[]>();

    void OnEnable()
    {
        if (modelAsset == null) return;

        model = Unity.InferenceEngine.ModelLoader.Load(modelAsset);
        worker = new Unity.InferenceEngine.Worker(model, Unity.InferenceEngine.BackendType.GPUCompute);
        rt = new RenderTexture(SIZE, SIZE, 0);
        ResetSmoothing();
    }

    /// <summary>Returns (emotionLabel, confidence 0-1). Pass a Texture containing a cropped face.</summary>
    public (string label, float confidence) PredictEmotion(Texture faceCrop)
    {
        if (worker == null) return (string.Empty, 0f);

        float[] data = Preprocess(faceCrop);                       
        using var input = new Unity.InferenceEngine.Tensor<float>(new Unity.InferenceEngine.TensorShape(1, 3, SIZE, SIZE), data);
        worker.Schedule(input);

        using var output = (worker.PeekOutput() as Unity.InferenceEngine.Tensor<float>).ReadbackAndClone();
        float[] probs = Softmax(output.DownloadToArray());         
        probs = Smooth(probs);                                    

        int best = 0;
        for (int i = 1; i < probs.Length; i++) if (probs[i] > probs[best]) best = i;

        EmotionFeedBack(EMOTIONS[best]);

        return (EMOTIONS[best], probs[best]);
    }


    public void EmotionFeedBack(string getEmotion)
    {
        Debug.Log(getEmotion);

        if (System.Enum.TryParse(getEmotion, out Emotions emotion))
        {
            CurrentEmotion = emotion;
        }
    }

    float[] Preprocess(Texture src)
    {
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(SIZE, SIZE, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, SIZE, SIZE), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        Color32[] c = tex.GetPixels32();
        float[] data = new float[3 * SIZE * SIZE];
        int plane = SIZE * SIZE;
        for (int y = 0; y < SIZE; y++)
            for (int x = 0; x < SIZE; x++)
            {
                Color32 p = c[(SIZE - 1 - y) * SIZE + x];     // flip Y (ReadPixels is bottom-up)
                int idx = y * SIZE + x;
                data[0 * plane + idx] = (p.r / 255f - MEAN[0]) / STD[0];   // R
                data[1 * plane + idx] = (p.g / 255f - MEAN[1]) / STD[1];   // G
                data[2 * plane + idx] = (p.b / 255f - MEAN[2]) / STD[2];   // B
            }
        Destroy(tex);
        return data;
    }

    float[] Smooth(float[] probs)
    {
        if (smoothingFrames <= 1) return probs;
        history.Enqueue(probs);
        while (history.Count > smoothingFrames) history.Dequeue();
        float[] avg = new float[probs.Length];
        foreach (var h in history)
            for (int i = 0; i < avg.Length; i++) avg[i] += h[i];
        for (int i = 0; i < avg.Length; i++) avg[i] /= history.Count;
        return avg;
    }

    public void ResetSmoothing() => history.Clear();   

    static float[] Softmax(float[] z)
    {
        float max = float.NegativeInfinity;
        foreach (var v in z) if (v > max) max = v;
        float sum = 0f; var e = new float[z.Length];
        for (int i = 0; i < z.Length; i++) { e[i] = Mathf.Exp(z[i] - max); sum += e[i]; }
        for (int i = 0; i < e.Length; i++) e[i] /= sum;
        return e;
    }

    void OnDisable()
    {
        worker?.Dispose();
        worker = null;

        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
            rt = null;
        }

        history.Clear();
    }
}
