#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

namespace ChiselPeak
{
    /// Editor-only: renders the player's view (tool and HUD included) into a portrait render texture
    /// at a fixed game-time step and writes numbered JPGs, for filming the gameplay video.
    public class FrameCapture : MonoBehaviour
    {
        public static FrameCapture Current;
        public int Frames { get; private set; }
        string folder; RenderTexture rt; Texture2D tex; Camera cam, toolCam; Canvas hud;
        bool running;

        public static FrameCapture Begin(string dir, int width, int height, int fps)
        {
            if (Current != null) Current.End();
            var g = ChiselGame.I;
            var fc = g.gameObject.AddComponent<FrameCapture>();
            fc.folder = dir;
            Directory.CreateDirectory(dir);
            foreach (var f in Directory.GetFiles(dir, "*.jpg")) File.Delete(f);
            fc.rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "capture" };
            fc.tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            fc.cam = g.Player.Cam;
            fc.toolCam = fc.cam.transform.Find("ToolCamera").GetComponent<Camera>();
            fc.cam.targetTexture = fc.rt;
            fc.hud = g.Hud.GetComponent<Canvas>();
            Art.SetLayer(fc.hud.gameObject, 5);
            fc.hud.renderMode = RenderMode.ScreenSpaceCamera;
            fc.hud.worldCamera = fc.cam;
            fc.hud.planeDistance = 0.2f;
            Time.captureFramerate = fps;
            fc.running = true;
            fc.StartCoroutine(fc.Loop());
            Current = fc;
            return fc;
        }

        IEnumerator Loop()
        {
            var wait = new WaitForEndOfFrame();
            while (running)
            {
                yield return wait;
                if (!running) break;
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
                tex.Apply(false);
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(folder, $"f{Frames:00000}.jpg"), tex.EncodeToJPG(93));
                Frames++;
            }
        }

        public int End()
        {
            running = false;
            Time.captureFramerate = 0;
            if (cam != null) cam.targetTexture = null;
            if (hud != null) { hud.renderMode = RenderMode.ScreenSpaceOverlay; }
            if (Current == this) Current = null;
            int n = Frames;
            Destroy(this);
            return n;
        }

        void OnDestroy()
        {
            if (rt != null) rt.Release();
        }
    }
}
#endif
