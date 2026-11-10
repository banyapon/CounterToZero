using System.Collections.Generic;
using UnityEngine;

namespace CountToZero
{
    /// The two detached arms of the giant bosses, laid out on the arena floor (XZ) from the shoulders.
    /// Flail: chain arms spin around the giant like a run-and-gun boss; the player dodges links and the mace.
    /// Whip: arms hang, wind up toward the player (warning line), slam straight down and leave a shock line.
    public sealed class GiantArms
    {
        const int Links = 8, Sparks = 14;
        const float HandY = .55f;
        sealed class Arm
        {
            public readonly List<SpriteRenderer> links = new();
            public SpriteRenderer tip;
            public float side, angle, length = 1.8f, timer;
            public int state; // whip: 0 idle, 1 wind-up, 2 slam, 3 shock, 4 retract
            public Vector3 target;
        }
        readonly Arm[] arms = new Arm[2];
        readonly List<SpriteRenderer> sparks = new();
        readonly Sprite[] shock;
        readonly float scale;
        bool whip;
        float nextStrike;
        int nextArm;

        public GiantArms(Transform parent, float scale, Sprite[] shock)
        {
            this.scale = scale; this.shock = shock;
            for (int a = 0; a < 2; a++)
            {
                var arm = arms[a] = new Arm { side = a == 0 ? -1 : 1 };
                for (int i = 0; i < Links; i++) arm.links.Add(Renderer(parent, "Giant arm link", 4));
                arm.tip = Renderer(parent, "Giant arm tip", 5);
            }
            for (int i = 0; i < Sparks; i++) sparks.Add(Renderer(parent, "Whip shock", 6));
            ResetRun();
        }

        static SpriteRenderer Renderer(Transform parent, string name, int order)
        {
            var r = new GameObject(name).AddComponent<SpriteRenderer>();
            r.transform.SetParent(parent, false); r.sortingOrder = order; r.enabled = false; return r;
        }

        public void ResetRun()
        {
            nextStrike = 0; nextArm = 0;
            foreach (var arm in arms) { arm.state = 0; arm.timer = 0; arm.length = 1.8f; arm.angle = arm.side * 40; }
        }

        public void Configure(bool whipArms, Sprite link, Sprite tip)
        {
            whip = whipArms; ResetRun();
            foreach (var arm in arms) { foreach (var l in arm.links) l.sprite = link; arm.tip.sprite = tip; }
        }

        public void Hide()
        {
            foreach (var arm in arms) { foreach (var l in arm.links) l.enabled = false; arm.tip.enabled = false; }
            foreach (var s in sparks) s.enabled = false;
        }

        static Vector3 Dir(float degrees) { float r = degrees * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(r), 0, -Mathf.Cos(r)); }
        static float AngleTo(Vector3 from, Vector3 to) { var d = to - from; return Mathf.Atan2(d.x, -d.z) * Mathf.Rad2Deg; }

        public void Tick(GameManager g, Vector3 bossPosition, float shoulderY, float shoulderX)
        {
            bool live = g.State == GameState.Playing && !g.Training && !g.CutIn.Visible;
            float dt = g.State == GameState.Playing ? Time.deltaTime : 0;
            int phase = Mathf.Max(1, g.Phase);
            var view = g.View.transform.rotation;
            bool sparking = false;

            foreach (var arm in arms)
            {
                Vector3 shoulder = bossPosition + Vector3.right * arm.side * shoulderX; shoulder.y = 0;
                float lag = 0;
                if (!whip)
                {
                    // Contra-style flail: both arms spin in opposite directions while the chain stretches in and out.
                    float spin = 70 + 22 * (phase - 1);
                    if (live) arm.angle += -arm.side * spin * dt; else arm.angle = Mathf.LerpAngle(arm.angle, arm.side * 40, dt * 3);
                    arm.timer += dt;
                    arm.length = live ? 2.3f + 1.5f * (.5f + .5f * Mathf.Sin(arm.timer * 1.1f + arm.side)) : Mathf.Lerp(arm.length, 1.8f, dt * 3);
                    lag = live ? -arm.side * 28 : 0;
                }
                else StepWhip(g, arm, shoulder, live, dt, phase);

                // Lay out links and tip from the shoulder down toward the floor.
                for (int i = 0; i <= Links; i++)
                {
                    float t = (i + 1f) / (Links + 1f);
                    var dir = Dir(arm.angle - lag * t);
                    var p = shoulder + dir * arm.length * t; p.y = Mathf.Lerp(shoulderY, HandY, t);
                    var r = i < Links ? arm.links[i] : arm.tip;
                    r.enabled = true; r.transform.position = p; r.transform.localScale = Vector3.one * scale;
                    r.transform.rotation = view * Quaternion.Euler(0, 0, i < Links ? 0 : AngleTo(Vector3.zero, dir) + 180);
                    r.color = Color.white;
                    if (live && Hits(g, p, i < Links ? .36f : (whip ? .5f : .62f)))
                        g.Player.Hurt(false, whip ? "Whip arm" : "Chain flail");
                }
                if (whip && (arm.state == 1 || arm.state == 3)) { DrawShockLine(g, shoulder, arm, view); sparking = true; }
            }
            if (!sparking) foreach (var s in sparks) s.enabled = false;
            if (whip && live && Time.time >= nextStrike && arms[0].state == 0 && arms[1].state == 0)
            {
                var arm = arms[nextArm]; nextArm = 1 - nextArm;
                arm.state = 1; arm.timer = 0; arm.target = g.Player.transform.position;
                GameAudio.Play(SoundCue.Warning);
            }
        }

        static bool Hits(GameManager g, Vector3 p, float radius)
        {
            var player = g.Player.transform.position; p.y = player.y = 0;
            return (player - p).sqrMagnitude < radius * radius;
        }

        void StepWhip(GameManager g, Arm arm, Vector3 shoulder, bool live, float dt, int phase)
        {
            arm.timer += dt;
            float windUp = Mathf.Max(.55f, .95f - .12f * (phase - 1)), reach = 7.5f;
            float aim = AngleTo(shoulder, arm.target);
            switch (arm.state)
            {
                case 0: // hang and sway
                    arm.angle = Mathf.LerpAngle(arm.angle, arm.side * 35 + Mathf.Sin(Time.time * 2 + arm.side) * 8, dt * 4);
                    arm.length = Mathf.Lerp(arm.length, 1.8f, dt * 4);
                    if (!live) arm.timer = 0;
                    break;
                case 1: // wind up: pull back away from the target, warning line flickers on the floor
                    arm.angle = Mathf.LerpAngle(arm.angle, aim + 180, dt * 6);
                    arm.length = Mathf.Lerp(arm.length, 1.2f, dt * 6);
                    if (arm.timer >= windUp) { arm.state = 2; arm.timer = 0; arm.angle = aim; }
                    break;
                case 2: // slam: snap straight along the line
                    arm.angle = aim; arm.length = Mathf.Lerp(1.2f, reach, arm.timer / .1f);
                    if (arm.timer >= .1f) { arm.state = 3; arm.timer = 0; arm.length = reach; GameAudio.Play(SoundCue.Explosion); }
                    break;
                case 3: // electric shock along the whole arm
                    var tip = shoulder + Dir(arm.angle) * arm.length;
                    if (live && BulletPattern.SegmentDistance(new Vector3(g.Player.transform.position.x, 0, g.Player.transform.position.z), shoulder, tip) < .55f)
                        g.Player.Hurt(false, "Whip shock");
                    if (arm.timer >= .7f) { arm.state = 4; arm.timer = 0; }
                    break;
                case 4: // retract
                    arm.length = Mathf.Lerp(arm.length, 1.8f, dt * 6);
                    if (arm.timer >= .4f) { arm.state = 0; arm.timer = 0; nextStrike = Time.time + Mathf.Max(.6f, 1.6f - .3f * (phase - 1)); }
                    break;
            }
        }

        void DrawShockLine(GameManager g, Vector3 shoulder, Arm arm, Quaternion view)
        {
            bool warning = arm.state == 1;
            float angle = warning ? AngleTo(shoulder, arm.target) : arm.angle;
            var dir = Dir(angle);
            for (int i = 0; i < sparks.Count; i++)
            {
                var s = sparks[i];
                var p = shoulder + dir * (.6f + i * .52f); p.y = .35f;
                s.enabled = shock != null && shock.Length > 0;
                if (!s.enabled) continue;
                s.sprite = shock[(int)(Time.time * 20 + i) % shock.Length];
                s.transform.position = p; s.transform.localScale = Vector3.one * scale * (warning ? .7f : 1.1f);
                s.transform.rotation = view * Quaternion.Euler(0, 0, ((int)(Time.time * 15) + i) % 2 == 0 ? 0 : 180);
                bool on = ((int)(Time.time * 12) % 2) == 0;
                s.color = warning ? new Color(1, .3f, .35f, on ? .55f : .2f) : (on ? Color.white : new Color(.6f, .9f, 1f));
            }
        }
    }
}
