using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DitheringEffect : MonoBehaviour {

    private Texture2D buffer;

    private List<float> dither8x8;
    private int w = 0, h = 0;
    private int[] palette = {
        // Greyscale palette (just for now. This can be generated later on...)
        0x000000, 0x111111, 0x222222, 0x333333,
        0x444444, 0x555555, 0x666666, 0x777777,
        0x888888, 0x999999, 0xaaaaaa, 0xbbbbbb,
        0xcccccc, 0xdddddd, 0xeeeeee, 0xffffff
    };

    private void OnEnable () {
        // Initialise bayer dither matrix
        ReinitialiseMatrix();
        ReinitialiseTex2D();
	}

    private void OnDisable () {
        CleanupTex2D();
    }

    private void ReinitialiseTex2D () {
        buffer = new Texture2D(Screen.width, Screen.height);
    }

    private void ReinitialiseMatrix () {
        dither8x8 = new List<float>();
        dither8x8.Clear();
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                dither8x8.Add(CalcDitherThreshold((x & 7) + ((y & 7) << 3)));
            }
        }
    }

    private void CleanupTex2D () {
#if UNITY_EDITOR
        if (buffer != null) { DestroyImmediate(buffer); }
#else
        if (buffer != null) { Destroy(buffer); }
#endif
    }

    private void OnRenderImage (RenderTexture source, RenderTexture destination) {
        w = source.width;
        h = source.height;

        if (buffer == null) {
            Graphics.Blit(source, destination);
            return;
        }

        if (dither8x8 == null || dither8x8.Count < 1) {
            ReinitialiseMatrix();
        }

        // Check if size is correct:
        if (buffer.width != w || buffer.height != h) { ReinitialiseTex2D(); }

        // Write some pixels to the rendertexture to see if this actually works or not kms
        int[] threshold = { 64, 64, 64 }; // 256 / 4
        RenderTexture.active = source;
        buffer.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        for (int y = 0; y < h; ++y) {
            for (int x = 0; x < w; ++x) {
                /*
                int idx = (x & 7) + ((y & 7) << 3);
                float factor = dither8x8[idx];
                Color c = buffer.GetPixel(x, y);
                float r = Mathf.Clamp(((c.r * 255.0f) + factor * threshold[0]), 0, 255) / 255.0f;
                float g = Mathf.Clamp(((c.g * 255.0f) + factor * threshold[1]), 0, 255) / 255.0f;
                float b = Mathf.Clamp(((c.b * 255.0f) + factor * threshold[2]), 0, 255) / 255.0f;

                Color result = GetClosestPaletteColour(new Color(r, g, b));
                */
                Color result = GetClosestPaletteColour(buffer.GetPixel(x, y));
                buffer.SetPixel(x, y, result);
            }
        }
        buffer.Apply();
        RenderTexture.active = null;
        Graphics.Blit(buffer, destination);
    }

    private static float CalcDitherThreshold (int p) {
        int q = p ^ (p >> 3);
        float ret = (float)
            (((p & 4) >> 2) | ((q & 4) >> 1)
           | ((p & 2) << 1) | ((q & 2) << 2)
           | ((p & 1) << 4) | ((q & 1) << 5));
        return ret / 64.0f;
    }

    private static int ColourDiff (Color a, Color b) {
        int diffR = Mathf.RoundToInt((a.r * 255.0f) - (b.r * 255.0f)),
            diffG = Mathf.RoundToInt((a.g * 255.0f) - (b.g * 255.0f)),
            diffB = Mathf.RoundToInt((a.b * 255.0f) - (b.b * 255.0f));
        return diffR * diffR + diffG * diffG + diffB * diffB;
    }

    private Color GetClosestPaletteColour (Color target) {
        Color close = ColourFromHex(palette[0]);
        foreach (int i in palette) {
            Color c = Colour255(
                (i >> 16) & 0xFF,
                (i >> 8) & 0xFF,
                i & 0xFF);
            if (ColourDiff(c, target) < ColourDiff(close, target)) {
                close = c;
            }
        }
        return close;
    }

    private Color ColourFromHex(int hex) {
        return new Color(
            (hex >> 16) & 0xFF,
            (hex >> 8) & 0xFF,
            hex & 0xFF
        );
    }

    private Color Colour255 (int r, int g, int b) {
        return new Color(
            r / 255.0f,
            g / 255.0f,
            b / 255.0f
        );
    }
}
