using UnityEngine;

namespace CountToZero
{
    /// Armored OMEGA attack loop: 8 long lasers (warning, then fire), then a short-range rush at the player, rest, repeat.
    public sealed class OmegaLaser
    {
        const int Rays = 8;
        const float Length = 16, Charge = 1f, Fire = .75f, Chase = 1.6f, Rest = 1.1f;
        readonly LineRenderer[] rays = new LineRenderer[Rays];
        float clock, spin;
        int step; // 0 charge, 1 fire, 2 chase, 3 rest
        Vector3 rushPosition;
        public bool Rushing => step == 2;

        public OmegaLaser(Transform parent, Material material)
        {
            for (int i = 0; i < Rays; i++)
            {
                var l = new GameObject("Omega laser " + i).AddComponent<LineRenderer>();
                l.transform.SetParent(parent, false);
                l.sharedMaterial = material; l.useWorldSpace = true; l.positionCount = 2;
                l.numCapVertices = 0; l.alignment = LineAlignment.View; l.sortingOrder = 7; l.enabled = false;
                rays[i] = l;
            }
            ResetRun();
        }

        public void ResetRun() { clock = 0; step = 3; spin = 0; Hide(); }
        public void Hide() { foreach (var r in rays) r.enabled = false; }

        /// Returns the position the boss should take this frame (rush), or null to leave BossController's motion alone.
        public Vector3? Tick(GameManager g, Vector3 boss)
        {
            bool live = g.State == GameState.Playing && !g.CutIn.Visible;
            if (!live) { Hide(); return null; }
            int phase = Mathf.Max(1, g.Phase);
            clock += Time.deltaTime;
            float duration = step == 0 ? Charge - .15f * (phase - 1) : step == 1 ? Fire : step == 2 ? Chase : Rest;
            if (clock >= duration)
            {
                clock = 0; step = (step + 1) % 4;
                if (step == 0) { spin += 22.5f; GameAudio.Play(SoundCue.Warning); }
                if (step == 1) GameAudio.Play(SoundCue.Explosion);
                if (step == 2) rushPosition = boss;
            }

            Vector3 origin = boss; origin.y = .6f;
            bool show = step == 0 || step == 1;
            bool flicker = (int)(Time.time * 14) % 2 == 0;
            for (int i = 0; i < Rays; i++)
            {
                var r = rays[i]; r.enabled = show;
                if (!show) continue;
                float a = (spin + i * 45) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                r.SetPosition(0, origin); r.SetPosition(1, origin + dir * Length);
                if (step == 0)
                {
                    r.widthMultiplier = .06f;
                    r.startColor = r.endColor = flicker ? new Color(1, .2f, .3f, .8f) : new Color(1, .2f, .3f, .25f);
                }
                else
                {
                    r.widthMultiplier = .38f + (flicker ? .06f : 0);
                    r.startColor = r.endColor = flicker ? Color.white : ArenaVisuals.Red;
                    var p = g.Player.transform.position; p.y = 0;
                    var o = origin; o.y = 0;
                    if (BulletPattern.SegmentDistance(p, o, o + dir * Length) < .45f) g.Player.Hurt(false, "Omega 8-way laser");
                }
            }

            if (step != 2) return null;
            // Short rush: chase the player at speed for a moment, then BossController eases it back home.
            var target = g.Player.transform.position; target.y = boss.y;
            rushPosition = Vector3.MoveTowards(rushPosition, target, Time.deltaTime * (6.5f + phase));
            rushPosition.x = Mathf.Clamp(rushPosition.x, -9.5f, 9.5f); rushPosition.z = Mathf.Clamp(rushPosition.z, -6.5f, 6.3f);
            var pp = g.Player.transform.position; pp.y = rushPosition.y;
            if ((pp - rushPosition).sqrMagnitude < 1.1f * 1.1f) g.Player.Hurt(false, "Omega rush");
            return rushPosition;
        }
    }
}
