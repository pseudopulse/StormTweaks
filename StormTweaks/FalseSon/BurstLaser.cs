using System;

namespace StormTweaks {
    public class BurstLaserCharge : BaseSkillState {
        public float maxChargeDuration = 2f;
        public float minChargeDuration = 0.4f;
        public float chargePercentage;
        public LineRenderer lineRenderer;
        public Transform head;
        private uint sound;
        private GameObject chargeEffect;

        public override void OnEnter()
        {
            base.OnEnter();

            maxChargeDuration /= base.attackSpeedStat;
            minChargeDuration /= base.attackSpeedStat;

            head = FindModelChild("Head");

            GameObject golemLaser = GameObject.Instantiate(Paths.GameObject.LaserTriLaser, transform.position, transform.rotation);
            chargeEffect = GameObject.Instantiate(Paths.GameObject.ChargeTriLaser, head.transform.position, head.transform.rotation);
            ScaleParticleSystemDuration duration = chargeEffect.GetComponent<ScaleParticleSystemDuration>();
            duration.newDuration = maxChargeDuration;

            lineRenderer = golemLaser.GetComponent<LineRenderer>();

            sound = AkSoundEngine.PostEvent(Events.Play_golem_laser_charge, base.gameObject);

            PlayCrossfade("Gesture, Head, Override", "FireLaserLoop", 0.25f);
		    PlayCrossfade("Gesture, Head, Additive", "FireLaserLoop", 0.25f);
        }

        public override void Update()
        {
            base.Update();

            if ((base.fixedAge >= minChargeDuration && !inputBank.skill4.down) || base.fixedAge >= maxChargeDuration + 0.2f) {
                outer.SetNextState(new BurstLaser(chargePercentage));
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            base.characterBody.SetAimTimer(0.2f);

            chargePercentage = Mathf.Clamp01(base.fixedAge / maxChargeDuration);

            chargeEffect.transform.position = head.transform.position;
            chargeEffect.transform.rotation = head.transform.rotation;

            lineRenderer.SetPosition(0, head.position);
            lineRenderer.SetPosition(1, FalseSon.GetOutPos(base.GetAimRay()));
            float perct = Mathf.Clamp(1.5f * (1f - chargePercentage), 0.1f, 1.5f);
            lineRenderer.startWidth = perct;
            lineRenderer.endWidth = perct;
            Color color = lineRenderer.material.GetColor("_TintColor");
            lineRenderer.material.SetColor("_TintColor", new Color(color.r, color.g, color.b, Mathf.Clamp01(chargePercentage * 1.75f)));
        }

        public override void OnExit()
        {
            base.OnExit();

            if (lineRenderer) {
                GameObject.Destroy(lineRenderer.gameObject);
            }

            if (chargeEffect) {
                GameObject.Destroy(chargeEffect);
            }

            AkSoundEngine.StopPlayingID(sound);

            PlayAnimation("Gesture, Head, Override", "FireLaserLoopEnd");
		    PlayAnimation("Gesture, Head, Additive", "FireLaserLoopEnd");
        }
    }

    public class BurstLaser : BaseSkillState {
        private float damageCoefficient;
        private float radius;
        private float blastRadius;
        public BurstLaser(float charge) {
            damageCoefficient = Util.Remap(charge, 0f, 1f, 8f, 26f);
            radius = Util.Remap(charge, 0f, 1f, 2f, 4f);
            blastRadius = Util.Remap(charge, 0f, 1f, 10f, 15f);
        }

        public override void OnEnter()
        {
            base.OnEnter();

            AkSoundEngine.PostEvent(Events.Play_golem_laser_fire, base.gameObject);
            Vector3 pos = FalseSon.GetOutPos(base.GetAimRay());

            if (base.isAuthority) {
                BlastAttack attack = new();
                attack.attacker = base.gameObject;
                attack.radius = blastRadius;
                attack.baseDamage = base.damageStat * damageCoefficient;
                attack.procCoefficient = 1f;
                attack.damageType = DamageType.Stun1s;
                attack.crit = base.RollCrit();
                attack.falloffModel = BlastAttack.FalloffModel.None;
                attack.teamIndex = base.GetTeam();
                attack.position = pos;

                attack.Fire();
            }

            PlayCrossfade("Gesture, Head, Override", "FireLaserLoop", 0.25f);
		    PlayCrossfade("Gesture, Head, Additive", "FireLaserLoop", 0.25f);

            Transform head = FindModelChild("MuzzleLaser");

            EffectData data = new() {
                origin = pos,
                start = head.transform.position,
                scale = radius
            };
            data.SetChildLocatorTransformReference(base.gameObject, GetModelChildLocator().FindChildIndex("MuzzleLaser"));
            EffectManager.SpawnEffect(Paths.GameObject.TracerTriLaser, data, false);
            EffectManager.SpawnEffect(Paths.GameObject.SojournExplosionVFX, new EffectData() {
                scale = blastRadius,
                origin = pos
            }, false);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= 0.3f) {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            PlayAnimation("Gesture, Head, Override", "FireLaserLoopEnd");
		    PlayAnimation("Gesture, Head, Additive", "FireLaserLoopEnd");
        }
    }
}