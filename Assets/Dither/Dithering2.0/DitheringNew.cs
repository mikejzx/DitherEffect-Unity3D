using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DitheringNew : MonoBehaviour {

    public ComputeShader shader;

    private int w = 0;
    private int h = 0;
    private RenderTexture tex;
    private readonly int downsample = 1;
    private readonly float referenceResX = 240;
    private float multiplier;

    [HideInInspector] private ComputeBuffer paletteBuffer;
    private List<int> palette = new List<int>() {
        // Greyscale palette (just for now. This can be generated later on...)
        /*0x000000, 0x111111, 0x222222, 0x333333,
        0x444444, 0x555555, 0x666666, 0x777777,
        0x888888, 0x999999, 0xaaaaaa, 0xbbbbbb,
        0xcccccc, 0xdddddd, 0xeeeeee, 0xffffff*/

        // RGB Palette from Bisqwits dithering article.
        0x080000, 0x201A0B, 0x432817, 0x492910,  
        0x234309, 0x5D4F1E, 0x9C6B20, 0xA9220F,
        0x2B347C, 0x2B7409, 0xD0CA40, 0xE8A077,
        0x6A94AB, 0xD5C4B3, 0xFCE76E, 0xFCFAE2
    };

    [HideInInspector] private ComputeBuffer dither8x8Buffer;
    private List<float> dither8x8;

    private void OnEnable () {
        // Query support; disable instance if not supported
        if (!SystemInfo.supportsComputeShaders) {
            enabled = false;
            Debug.Log("Compute shaders not supported on this platform...");
            return;
        }

        multiplier = Screen.width / referenceResX;

        // Initialise palette in RGB
        /*palette = new List<int>();
        palette.Clear();
        for (int r0 = 0; r0 < 257; r0 += 64) {
            var r = Mathf.Clamp(r0, 0, 255);
            for (int g0 = 0; g0 < 257; g0 += 64) {
                var g = Mathf.Clamp(g0, 0, 255);
                for (int b0 = 0; b0 < 257; b0 += 64) {
                    var b = Mathf.Clamp(b0, 0, 255);
                    var c = ((r << 16) + (g << 8) + b) & 0xFFFFFF;
                    palette.Add(c);
                }
            }
        }*/

        // Initialise palette buffer
        paletteBuffer = new ComputeBuffer(palette.Count, sizeof(int));
        paletteBuffer.SetData(palette);

        ReinitialiseMatrix();

        dither8x8Buffer = new ComputeBuffer(64, sizeof(float));
        dither8x8Buffer.SetData(dither8x8);

        InitialiseTex();
    }

    private void OnDisable () {
        paletteBuffer.Release();
        dither8x8Buffer.Release();

        ReleaseTex();
    }

    private void InitialiseTex () {
        if (w == 0 || h == 0) { return; }
        ReleaseTex();
        tex = new RenderTexture(w, h, 8);
        tex.enableRandomWrite = true;
        tex.filterMode = FilterMode.Point;
        tex.Create();
    }

    private void ReleaseTex () {
        if (tex != null) { tex.Release(); }
    }

    private void ReinitialiseMatrix () {
        if (dither8x8 == null) {
            dither8x8 = new List<float>();
        }
        dither8x8.Clear();
        for (int y = 0; y < 8; y++) {
            for (int x = 0; x < 8; x++) {
                dither8x8.Add(CalcDitherThreshold(x + y * 8));
            }
        }
    }

    private void OnRenderImage (RenderTexture source, RenderTexture destination) {
        w = Screen.width >> downsample; //(int)((Screen.width >> downsample) * multiplier);
        h = Screen.height >> downsample; //(int)((Screen.height >> downsample) * multiplier);
        if (tex == null || tex.width == 0 || tex.height == 0
            || tex.width != w || tex.height != h) {
            multiplier = Screen.width / referenceResX;
            InitialiseTex();
        }
        if (dither8x8 == null || dither8x8.Count < 1) {
            Debug.Log("Bayer Matrix not initialised");
            ReinitialiseMatrix();
        }

        int kernel = shader.FindKernel("CSMain");
        shader.SetInt("Downsample", downsample);
        shader.SetFloat("Multiplier", multiplier);
        shader.SetTexture(kernel, "Result", tex);
        shader.SetTexture(kernel, "ImageInput", source);

        shader.SetBuffer(kernel, "Palette", paletteBuffer);
        shader.SetInt("PaletteLength", palette.Count);
        shader.SetBuffer(kernel, "Dither8x8", dither8x8Buffer);

        shader.Dispatch(kernel, w / 8, h / 8, 1);

        Graphics.Blit(tex, destination);
    }

    private static float CalcDitherThreshold (int p) {
        int q = p ^ (p >> 3);
        float ret = (float)
            (((p & 4) >> 2) | ((q & 4) >> 1)
           | ((p & 2) << 1) | ((q & 2) << 2)
           | ((p & 1) << 4) | ((q & 1) << 5));
        return ret / 64.0f;
    }
}
