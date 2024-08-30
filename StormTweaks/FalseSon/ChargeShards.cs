using System;
using System.Collections;
using EntityStates.InfiniteTowerSafeWard;

namespace StormTweaks {
    public class ChargeShards : BaseSkillState {
        public int shardCount = 0;
        public Transform muzzle;
        public GameObject charge;
        public bool shot = false;
        public string sound;
        public override void OnEnter()
        {
            base.OnEnter();

            muzzle = FindModelChild("MuzzleRight");

            if (base.skillLocator.secondary.stock > 1) {
                charge = GameObject.Instantiate(Paths.GameObject.LunarWispBombChargeUp, muzzle.transform.position, muzzle.transform.rotation);
                charge.transform.localScale *= 0.25f;

                float duration = (0.35f / base.attackSpeedStat) * (Mathf.Min(6, skillLocator.secondary.stock));

                charge.GetComponent<ScaleParticleSystemDuration>().newDuration = duration;
            }
            else {
                shardCount = 1;
                base.skillLocator.secondary.DeductStock(1);
            }

            sound = new EntityStates.FalseSon.LunarSpikes().attackSoundString;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            base.characterBody.SetAimTimer(0.2f);

            if (charge) {
                charge.transform.position = muzzle.transform.position;
                charge.transform.rotation = muzzle.transform.rotation;
            }

            if (shot) return;

            if (base.skillLocator.secondary.stock <= 0 || shardCount >= 6) {
                base.characterBody.StartCoroutine(FireShards());
                shot = true;
                return;
            }

            if (!base.inputBank.skill2.down) {
                if (shardCount <= 0) {
                    base.skillLocator.secondary.DeductStock(1);
                    shardCount++;
                }
                
                base.characterBody.StartCoroutine(FireShards());
                shot = true;
                return;
            }

            PlayAnimation("Gesture, Override", "HoldGauntletsUp", "LunarSpike.playbackRate", 0.2f);

            if (shardCount < 6 && base.inputBank.skill2.down && base.fixedAge >= 0.35f / base.attackSpeedStat) {
                base.fixedAge = 0f;
                shardCount++;
                base.skillLocator.secondary.DeductStock(1);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }

        public override void OnExit()
        {
            base.OnExit();

            if (charge) GameObject.Destroy(charge);
        }

        public IEnumerator FireShards() {
            PlayCrossfade("Gesture, Additive", "FireLunarSpike", "LunarSpike.playbackRate", 0.3f, 0.1f);
			PlayCrossfade("Gesture, Override", "FireLunarSpike", "LunarSpike.playbackRate", 0.3f, 0.1f);

            float timePer = 0.15f / shardCount;

            float mult = (float)(shardCount == 1 ? 0 : shardCount) / 6f;

            if (charge) GameObject.Destroy(charge);

            GameObject proj = Paths.GameObject.LunarSpike;

            if (shardCount > 1) EffectManager.SimpleEffect(Paths.GameObject.LunarWispTrackingBombExplosion, muzzle.position, Quaternion.identity, false);

            for (int i = 0; i < shardCount; i++) {
                yield return new WaitForSeconds(timePer);
                EffectManager.SimpleEffect(Paths.GameObject.MuzzleflashLunarNeedle, muzzle.transform.position, muzzle.transform.rotation, false);

                AkSoundEngine.PostEvent(sound, base.gameObject);

                if (base.isAuthority) {
                    FireProjectileInfo info = new();
                    info.position = muzzle.transform.position;
                    info.rotation = Util.QuaternionSafeLookRotation(Util.ApplySpread(base.GetAimRay().direction, -5f, 5f, 1f, 1f));
                    info.damage = base.damageStat * 2f;
                    info.owner = base.gameObject;
                    info.crit = base.RollCrit();
                    info.projectilePrefab = proj;
                    
                    ProjectileManager.instance.FireProjectile(info);
                }
            }

            shardCount = 0;

            yield return new WaitForSeconds(0.5f / base.attackSpeedStat);

            outer.SetNextStateToMain();
        }
    }
}