using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DitherEffect : MonoBehaviour
{
    // The dither effect shader.
    public ComputeShader shader;

    // For forcing a screen resolution (for testing).
    public bool useForcedRes = false;
    public int forcedResX = 600;
    public int forcedResY = 480;

    // Output texture filtering mode.  Point/nearest filtering provides a nice
    // aesthetic but is subject to an artifact where the dither pattern is
    // incorrect.
    public FilterMode filterMode = FilterMode.Point;

    // When not using a forced resolution, this is how many times to downsample
    // the output texture.
    public int downsamples = 0;

    // Screen render texture.
    private RenderTexture tex;

    // Buffers passed to compute shader
    private ComputeBuffer dither8x8Buffer;
    private ComputeBuffer paletteBuffer;

    // The palette to reduce screen colours to.
    // TODO allow user to specify this in the inspector.
    private List<int> palette = new List<int>()
    {
        // Greyscale palette
        /*0x000000, 0x111111, 0x222222, 0x333333,
        0x444444, 0x555555, 0x666666, 0x777777,
        0x888888, 0x999999, 0xaaaaaa, 0xbbbbbb,
        0xcccccc, 0xdddddd, 0xeeeeee, 0xffffff*/

        // RGB Palette from Bisqwit's dithering article.
        0x080000, 0x201A0B, 0x432817, 0x492910,
        0x234309, 0x5D4F1E, 0x9C6B20, 0xA9220F,
        0x2B347C, 0x2B7409, 0xD0CA40, 0xE8A077,
        0x6A94AB, 0xD5C4B3, 0xFCE76E, 0xFCFAE2
    };

    private void OnEnable()
    {
        // Query support; disable the effect if not supported
        if (!SystemInfo.supportsComputeShaders)
        {
            enabled = false;
            Debug.LogWarning("Compute shaders are not supported on this system.  Dithering effect cannot be used.");
            return;
        }

        // Disable if shader is not provided.
        if (!shader)
        {
            enabled = false;
            return;
        }

        // Initialise an RGB palette.
        /*
        palette = new List<int>();
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
        }
        */

        // Create dithering matrix.
        List<float> dither8x8 = new List<float>(64);
        for (int i = 0; i < 64; i++)
            dither8x8.Add(CalcDitherThreshold(i));

        // Initialise palette buffer
        paletteBuffer = new ComputeBuffer(palette.Count, sizeof(int));
        paletteBuffer.SetData(palette);

        // Initialise dithering matrix buffer
        dither8x8Buffer = new ComputeBuffer(64, sizeof(float));
        dither8x8Buffer.SetData(dither8x8);
    }

    private void OnDisable()
    {
        if (paletteBuffer != null)
            paletteBuffer.Release();

        if (dither8x8Buffer != null)
            dither8x8Buffer.Release();

        if (tex != null)
            RenderTexture.ReleaseTemporary(tex);
    }

    private static float CalcDitherThreshold(int p)
    {
        int q = p ^ (p >> 3);
        float ret = (float)
            (((p & 4) >> 2) | ((q & 4) >> 1)
           | ((p & 2) << 1) | ((q & 2) << 2)
           | ((p & 1) << 4) | ((q & 1) << 5));
        return ret / 64.0f;
    }

    // Perform dither post-processing on the given image.
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        int w, h;

        w = source.width;
        h = source.height;

        if (useForcedRes)
        {
            if (forcedResX > 0)
                w = forcedResX;

            if (forcedResY > 0)
                h = forcedResY;
        }
        else
        {
            w = source.width >> downsamples;
            h = source.height >> downsamples;
        }


        // Skip the effect if the image is too small.
        if (w < 8 || h < 8)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // Get a temporary render texture.
        tex = RenderTexture.GetTemporary(w, h, source.depth, source.format);
        tex.enableRandomWrite = true;
        tex.filterMode = filterMode;
        tex.Create();

        // Get the kernel in the shader.
        int kernel = shader.FindKernel("CSMain");


        // Set the input and output textures.
        shader.SetTexture(kernel, "Result", tex);
        shader.SetTexture(kernel, "ImageInput", source);

        // Set the texture properties.
        shader.SetInt("ImageInputW", source.width);
        shader.SetInt("ImageInputH", source.height);
        shader.SetInt("ResultW", tex.width);
        shader.SetInt("ResultH", tex.height);

        // Set the palette and dither matrix.
        shader.SetBuffer(kernel, "Palette", paletteBuffer);
        shader.SetInt("PaletteLength", palette.Count);
        shader.SetBuffer(kernel, "Dither8x8", dither8x8Buffer);

        // Run the shader.
        shader.Dispatch(kernel, w / 8, h / 8, 1);

        // Blit the processed texture to the screen.
        Graphics.Blit(tex, destination);

        RenderTexture.ReleaseTemporary(tex);
    }
}
