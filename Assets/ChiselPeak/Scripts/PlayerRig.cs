using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using GameTouch;

namespace ChiselPeak
{
    /// First-person player: walk, look, swing the current tool at the crosshair.
    public class PlayerRig : MonoBehaviour
    {
        public const int ViewLayer = 30;
        public const float EyeHeight = 1.65f;

        public Camera Cam { get; private set; }
        public float Yaw, Pitch;
        public int Swings { get; private set; }
        public ToolDef Tool { get; private set; }
        public System.Action<RaycastHit, ToolDef> OnSwingHit;
        public bool Locked; // no input (win screen)
        public TouchPad.Button SwingButton; // on-screen swing for phones

        // bot input
        public bool BotDriven;
        public Vector3 BotMove;   // world direction, magnitude 0..1
        public bool BotSwing;

        CharacterController cc;
        Transform eyes, pivot, view;
        float swingT = 1f; bool hitDone = true;
        float shake, bob, vy;
        Camera toolCam;

        public void Build(Vector3 pos, float yaw)
        {
            transform.position = pos;
            Yaw = yaw;
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.4f;

            eyes = new GameObject("Eyes").transform; eyes.SetParent(transform, false);
            eyes.localPosition = new Vector3(0, EyeHeight, 0);
            var camGo = new GameObject("PlayerCamera"); camGo.tag = "MainCamera";
            camGo.transform.SetParent(eyes, false);
            Cam = camGo.AddComponent<Camera>();
            Cam.fieldOfView = 70f; Cam.nearClipPlane = 0.08f; Cam.farClipPlane = 450f;
            Cam.cullingMask = ~(1 << ViewLayer);
            camGo.AddComponent<AudioListener>();
            var data = Cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var toolGo = new GameObject("ToolCamera");
            toolGo.transform.SetParent(camGo.transform, false);
            toolCam = toolGo.AddComponent<Camera>();
            toolCam.fieldOfView = 60f; toolCam.nearClipPlane = 0.02f; toolCam.farClipPlane = 10f;
            toolCam.cullingMask = 1 << ViewLayer;
            toolCam.clearFlags = CameraClearFlags.Depth;
            var td = toolCam.GetUniversalAdditionalCameraData();
            td.renderType = CameraRenderType.Overlay;
            data.cameraStack.Add(toolCam);

            pivot = new GameObject("ToolPivot").transform;
            pivot.SetParent(toolGo.transform, false);
            SetTool(0);
        }

        public void SetTool(int index)
        {
            Tool = Tools.All[index];
            if (view != null) Destroy(view.gameObject);
            view = Tools.BuildView(index, pivot).transform;
            swingT = 1f; hitDone = true;
            raise = 1f;
        }
        float raise;

        public void AddShake(float s) => shake = Mathf.Min(0.6f, shake + s);

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 moveWorld = Vector3.zero;
            bool swing = false;
            if (!Locked && !BotDriven)
            {
                var kb = Keyboard.current;
                var ms = TouchPad.RecentTouch ? null : Mouse.current; // ignore mouse events a browser emulates from taps
                if (ms != null)
                {
                    var d = ms.delta.ReadValue();
                    Yaw += d.x * 0.12f; Pitch -= d.y * 0.12f;
                    swing = ms.leftButton.isPressed;
                    if (ms.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
                }
                if (kb != null)
                {
                    Vector2 mv = Vector2.zero;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) mv.y += 1;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) mv.y -= 1;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) mv.x += 1;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) mv.x -= 1;
                    swing |= kb.spaceKey.isPressed;
                    var fwd = Quaternion.Euler(0, Yaw, 0);
                    moveWorld = fwd * new Vector3(mv.x, 0, mv.y);
                    if (moveWorld.sqrMagnitude > 1) moveWorld.Normalize();
                }
                var gp = Gamepad.current;
                if (gp != null)
                {
                    var l = gp.leftStick.ReadValue(); var r = gp.rightStick.ReadValue();
                    Yaw += r.x * 120f * dt; Pitch -= r.y * 90f * dt;
                    moveWorld += Quaternion.Euler(0, Yaw, 0) * new Vector3(l.x, 0, l.y);
                    swing |= gp.rightTrigger.isPressed || gp.buttonSouth.isPressed;
                }
                // phones: stick on the left to walk, drag elsewhere to look, hold SWING
                var tp = TouchPad.I;
                if (tp != null)
                {
                    if (tp.Dragging)
                    {
                        var d = tp.DragDelta / Mathf.Max(1f, Screen.width) * 110f;
                        Yaw += d.x; Pitch -= d.y;
                    }
                    if (tp.StickHeld)
                    {
                        moveWorld += Quaternion.Euler(0, Yaw, 0) * new Vector3(tp.Stick.x, 0, tp.Stick.y);
                        if (moveWorld.sqrMagnitude > 1) moveWorld.Normalize();
                    }
                    if (SwingButton != null) swing |= SwingButton.held || SwingButton.pressed;
                }
            }
            else if (BotDriven && !Locked)
            {
                moveWorld = BotMove;
                swing = BotSwing;
            }
            else if (BotDriven) moveWorld = BotMove;

            Pitch = Mathf.Clamp(Pitch, -75f, 80f);
            transform.rotation = Quaternion.Euler(0, Yaw, 0);
            eyes.localRotation = Quaternion.Euler(Pitch, 0, 0);

            vy = cc.isGrounded ? -1f : vy - 20f * dt;
            cc.Move((moveWorld * 4f + Vector3.up * vy) * dt);
            float speed = new Vector2(cc.velocity.x, cc.velocity.z).magnitude;
            bob += dt * speed * 2.2f;

            if (swingT >= 1f && swing && !Locked && raise <= 0f)
            {
                swingT = 0f; hitDone = false; Swings++;
            }
            if (swingT < 1f)
            {
                swingT = Mathf.Min(1f, swingT + dt / Tool.SwingTime);
                if (!hitDone && swingT >= 0.45f) { hitDone = true; DoHit(); }
            }
            raise = Mathf.Max(0, raise - dt * 2.5f);

            Pose(speed);

            shake = Mathf.Max(0, shake - dt * 1.8f);
            var s = shake * shake;
            Cam.transform.localPosition = new Vector3(Mathf.PerlinNoise(Time.time * 30f, 0) - 0.5f, Mathf.PerlinNoise(0, Time.time * 30f) - 0.5f, 0) * s * 0.9f;
            Cam.transform.localRotation = Quaternion.Euler((Mathf.PerlinNoise(Time.time * 25f, 7) - 0.5f) * s * 14f, 0, (Mathf.PerlinNoise(3, Time.time * 25f) - 0.5f) * s * 10f);
        }

        void Pose(float speed)
        {
            float t = swingT;
            float b = Mathf.Sin(bob) * Mathf.Min(1f, speed / 3f);
            Vector3 restPos = new Vector3(0.2f, -0.3f + b * 0.01f, 0.6f);
            Vector3 restRot = new Vector3(28f, -18f, -14f);
            Vector3 pos = restPos, rot = restRot;
            if (Tool.Thrust)
            {
                restPos = new Vector3(0.4f, -0.42f + b * 0.01f, 0.5f);
                restRot = new Vector3(6f, -30f, 0f);
                float back = Smooth(t, 0f, 0.38f) * (1 - Smooth(t, 0.38f, 0.47f));
                float fwd = Smooth(t, 0.38f, 0.47f) * (1 - Smooth(t, 0.6f, 1f));
                pos = restPos + new Vector3(0.03f, 0.01f, -0.15f) * back + new Vector3(-0.05f, 0.03f, 0.5f) * fwd;
                rot = restRot + new Vector3(-3f, 0, 0) * back + new Vector3(-2f, 4f, 0) * fwd;
            }
            else
            {
                float wind = Smooth(t, 0f, 0.33f) * (1 - Smooth(t, 0.33f, 0.45f));
                float strike = Smooth(t, 0.33f, 0.45f) * (1 - Smooth(t, 0.55f, 1f));
                pos = restPos + new Vector3(0.03f, 0.08f, -0.05f) * wind + new Vector3(-0.1f, 0.06f, 0.14f) * strike;
                rot = restRot + new Vector3(-55f, 6f, 6f) * wind + new Vector3(62f, 10f, 4f) * strike;
            }
            pos += new Vector3(0, -0.7f, 0) * raise * raise;
            pivot.localPosition = pos;
            pivot.localEulerAngles = rot;
        }

        static float Smooth(float t, float a, float b) => Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - a) / (b - a)));

        void DoHit()
        {
            var ray = new Ray(Cam.transform.position, Cam.transform.forward);
            int mask = ~((1 << ViewLayer) | (1 << 2));
            if (Physics.Raycast(ray, out var hit, Tool.Reach, mask, QueryTriggerInteraction.Ignore))
            {
                AddShake(Tool.Shake);
                OnSwingHit?.Invoke(hit, Tool);
            }
        }

        /// Where the swing would land right now (for the bot).
        public bool Aim(out RaycastHit hit)
        {
            var ray = new Ray(Cam.transform.position, Cam.transform.forward);
            int mask = ~((1 << ViewLayer) | (1 << 2));
            return Physics.Raycast(ray, out hit, 30f, mask, QueryTriggerInteraction.Ignore);
        }

        public Vector3 EyePos => eyes.position;
    }
}
