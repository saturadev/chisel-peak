using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using GameTouch;

namespace ChiselPeak
{
    /// Chisel Peak: chip Kindle, a giant ember salamander, out of a dome of ice.
    /// Builds the whole scene at start; R reloads it.
    public class ChiselGame : MonoBehaviour
    {
        public static ChiselGame I;
        public TMP_FontAsset Font;
        public Material FontOutline;
        public Shader LitShader, ParticleShader, SkyShader;
        public bool StartWithBot;

        public const float WinFraction = 0.46f;

        public IceBlock Block { get; private set; }
        public Creature Kindle { get; private set; }
        public PlayerRig Player { get; private set; }
        public Hud Hud { get; private set; }
        public ChiselBot Bot { get; private set; }
        public int Ice { get; private set; }
        public int ToolIndex { get; private set; }
        public bool Won { get; private set; }
        public float RunTime { get; private set; }
        public int Collapses { get; private set; }
        float winAt;

        public float Freed => Won ? 1f : Mathf.Clamp01(Block.Removed / (Block.Total * WinFraction));
        public ToolDef NextTool => ToolIndex + 1 < Tools.All.Length ? Tools.All[ToolIndex + 1] : null;
        public bool CanCraft => !Won && NextTool != null && Ice >= NextTool.Cost;

        void Awake()
        {
            I = this;
            Art.Init(LitShader, ParticleShader);
            var world = new GameObject("World").transform;
            var groundPlane = World.Build(world, SkyShader);
            StaticBatchingUtility.Combine(world.gameObject);

            var fx = new GameObject("Fx").AddComponent<Fx>();
            fx.Build(groundPlane);

            var iceRoot = new GameObject("IceDome");
            Kindle = new GameObject("Kindle").AddComponent<Creature>();
            Kindle.transform.SetParent(iceRoot.transform, false);
            Kindle.Build();
            Kindle.transform.localScale = Vector3.one * 0.8f;
            iceRoot.transform.localScale = Vector3.one * 1.25f;
            Block = iceRoot.AddComponent<IceBlock>();
            var iceMat = Art.Glass(new Color(0.4f, 0.75f, 1f, 0.55f), 0.88f);
            iceMat.name = "cp_ice";
            Block.Build(Kindle, iceMat);
            Block.OnShard = p => { if (!Won) { Ice++; Hud.SetIce(Ice); } };
            Block.OnCollapse = n => { Collapses++; Player.AddShake(Mathf.Min(0.5f, 0.12f + n * 0.01f)); };

            Player = new GameObject("Player").AddComponent<PlayerRig>();
            Player.Build(new Vector3(0.4f, 0, -19f), 0f);
            Player.Pitch = -12f;
            Player.OnSwingHit = OnSwingHit;
            Fx.Snowfall(Player.transform);

            Hud = new GameObject("Hud", typeof(RectTransform)).AddComponent<Hud>();
            Hud.Build(Font, FontOutline);
            Hud.SetIce(0);
            Hud.SetTool(Player.Tool, NextTool);
            Hud.SetFreed(0);

            Bot = Player.gameObject.AddComponent<ChiselBot>();
            Bot.Active = StartWithBot;

            // phones: stick on the left to walk, drag to look, hold SWING, tap the craft prompt, tap to dig again
            Touch = TouchPad.Create(Font, FontOutline);
            Touch.AddStick(new Rect(0, 0, 0.5f, 0.6f), new Vector2(0, 0), new Vector2(250, 520), 150);
            Player.SwingButton = Touch.AddButton("SWING", new Vector2(1, 0), new Vector2(-200, 360), 135, new Color(1f, 0.55f, 0.16f), 46);
            craftArea = Touch.AddArea(Hud.PromptRect);
        }

        public TouchPad Touch { get; private set; }
        TouchPad.Button craftArea;

        void OnSwingHit(RaycastHit hit, ToolDef tool)
        {
            var chunk = hit.collider.GetComponent<IceChunk>();
            if (chunk != null && !chunk.Falling && !Won)
                Block.Hit(chunk, hit.point, hit.normal, tool);
            else if (Fx.I != null)
                Fx.I.IceHit(hit.point, hit.normal, 0.2f);
        }

        public void Craft()
        {
            if (!CanCraft) return;
            Ice -= NextTool.Cost;
            ToolIndex++;
            Player.SetTool(ToolIndex);
            Hud.SetIce(Ice);
            Hud.SetTool(Player.Tool, NextTool);
            Hud.Banner(Player.Tool.Name.ToUpperInvariant() + "!");
        }

        public void GiveIce(int n) { Ice += n; Hud.SetIce(Ice); }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.rKey.wasPressedThisFrame) { Restart(); return; }
                if (kb.eKey.wasPressedThisFrame) Craft();
            }
            var gp = Gamepad.current;
            if (gp != null && gp.buttonNorth.wasPressedThisFrame) Craft();

            bool touch = TouchPad.Active;
            Touch.hidden = Bot.Active;
            Player.SwingButton.visible = !Won;
            if (craftArea.pressed) Craft();
            if (touch && Won && winAt < 0 && Hud.WinShown && Touch.Tapped) { Restart(); return; }

            if (!Won)
            {
                RunTime += Time.deltaTime;
                Hud.SetFreed(Freed);
                Hud.SetPrompt(CanCraft ? (touch ? "TAP  CRAFT " : "E  CRAFT ") + NextTool.Name.ToUpperInvariant() : null);
                if (Block.Removed >= Block.Total * WinFraction) Win();
            }
            else if (winAt >= 0 && Time.time >= winAt)
            {
                winAt = -1;
                Hud.ShowWin(RunTime, Player.Swings);
                if (TouchPad.Active) Hud.SetWinHint("TAP  DIG AGAIN");
            }
        }

        void Win()
        {
            Won = true;
            Hud.SetFreed(1f);
            Hud.SetPrompt(null);
            Player.AddShake(0.6f);
            Block.Burst(Kindle.transform.position + Vector3.up * 3f);
            Kindle.Wake();
            winAt = Time.time + 1.4f;
        }
    }
}
