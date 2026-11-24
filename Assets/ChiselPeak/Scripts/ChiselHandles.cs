#if UNITY_EDITOR && SATURA_SPACE
using System.Collections;
using SaturaSpace;
using UnityEngine;
#endif

namespace ChiselPeak
{
    public static class ChiselHandles
    {
#if UNITY_EDITOR && SATURA_SPACE
        [SaturaHandle("Reloads the scene for a fresh run. Yields the state once the new run is ready.")]
        public static IEnumerator StartRun()
        {
            if (ChiselGame.I != null) ChiselGame.I.Restart();
            yield return null; yield return null; yield return null;
            yield return GetState();
        }

        [SaturaHandle("Turns on the bot that plays the run by itself.")]
        public static string DemoRun()
        {
            ChiselGame.I.Bot.Active = true;
            return GetState();
        }

        [SaturaHandle("Waits until Kindle is freed or the time runs out. Yields the state.")]
        public static IEnumerator WaitForFreed(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end && !ChiselGame.I.Won) yield return null;
            yield return GetState();
        }

        [SaturaHandle("Run state: tool, ice, chunks removed, freed %, collapses, swings, time")]
        public static string GetState()
        {
            var g = ChiselGame.I;
            if (g == null) return "no game";
            return $"tool={g.Player.Tool.Name} ice={g.Ice} removed={g.Block.Removed}/{g.Block.Total} live={g.Block.Live.Count} falling={g.Block.FallingNow} " +
                   $"freed={g.Freed:P0} collapses={g.Collapses} lastCollapse={g.Block.LastCollapse} swings={g.Player.Swings} time={g.RunTime:F1} won={g.Won} pos={g.Player.transform.position}";
        }

        [SaturaHandle("Adds ice for testing upgrades")]
        public static string GiveIce(int n) { ChiselGame.I.GiveIce(n); return GetState(); }

        [SaturaHandle("Crafts the next tool if there is enough ice")]
        public static string Craft() { ChiselGame.I.Craft(); return GetState(); }

        [SaturaHandle("Starts filming the player's view (tool and HUD) to numbered JPGs in a folder, at a fixed game-time step")]
        public static string StartCapture(string folder, int fps)
        {
            FrameCapture.Begin(folder, 1080, 1920, fps);
            return "capturing to " + folder;
        }

        [SaturaHandle("Stops filming. Returns the frame count.")]
        public static string StopCapture()
        {
            int n = FrameCapture.Current != null ? FrameCapture.Current.End() : 0;
            return "frames=" + n;
        }

        [SaturaHandle("Waits for a number of frames to be captured (game time = frames / fps). Yields the state.")]
        public static IEnumerator WaitFrames(int frames)
        {
            var fc = FrameCapture.Current;
            int start = fc != null ? fc.Frames : 0;
            int guard = 0;
            while (fc != null && fc.Frames - start < frames && guard++ < 100000) yield return null;
            yield return GetState();
        }

        [SaturaHandle("Collapse test: breaks the two bottom layers across the front. Returns how many chunks lost support.")]
        public static string CollapseTest()
        {
            var b = ChiselGame.I.Block;
            int before = b.Removed;
            int broke = b.BreakCells(new Vector3Int(2, 0, 0), new Vector3Int(10, 1, 2));
            return $"broke={broke} fell={b.Removed - before - broke} " + GetState();
        }
#endif
    }
}
