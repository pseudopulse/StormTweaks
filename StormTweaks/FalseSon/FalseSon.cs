using System;
using EntityStates.FalseSon;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace StormTweaks {
    public class FalseSon {
        public static bool FalseSonBetterLaserVisuals => Bind<bool>("False Son: Laser of the Father", "Better Visuals", "Improves the visual effects of the laser.", true);
        public static bool FalseSonReworkedBurstLaser => Bind<bool>("False Son: Laser Burst", "Reworked Burst", "Rework Laser Burst to charge up a golden beam that blasts a stunning explosion for much more damage.", true);
        public static bool FalseSonLunarShotgun => Bind<bool>("False Son: Lunar Spikes", "Lunar Shotgun", "Make this skill chargable to blast a shotgun of up to 8 shards at a time.", true);
        public static bool FalseSonCoolerShards => Bind<bool>("False Son: Lunar Shards", "Seeking Shards", "Make shards seek slightly", true);
        public static bool FalseSonGrowthScaling => Bind<bool>("False Son: General", "Growth Scaling", "Allow growth to scale with level at 12.5% efficiency", true);
        public static bool FalseSonBetterSwing => Bind<bool>("False Son: Club of the Forsaken", "Better VFX", "Improves the visual effects of the swing.", true);
        public static bool FalseSonAgility => Bind<bool>("False Son: Club of the Forsaken", "Agile", "Make this skill agile", true);
        public static GameObject FalseSonShardGhost;
        public static GameObject FalseSonSwingEffect;
        public static GameObject FalseSonSwingEffect2;
        public static void Init() {
            if (FalseSonBetterLaserVisuals) {
                On.EntityStates.FalseSon.LaserFather.OnEnter += LaserFather_OnEnter;
                On.EntityStates.FalseSon.LaserFather.FixedUpdate += LaserFather_FixedUpdate;
                On.EntityStates.FalseSon.LaserFatherCharged.OnEnter += LaserFatherCharged_OnEnter;
                On.EntityStates.FalseSon.LaserFatherCharged.FixedUpdate += LaserFatherCharged_FixedUpdate;
                On.EntityStates.FalseSon.LaserFatherCharged.OnExit += LaserFatherCharged_OnExit;
            }

            if (FalseSonReworkedBurstLaser) {
                Paths.SkillFamily.FalseSonBodySpecialFamily.variants[1].skillDef.activationState = new(typeof(BurstLaserCharge));

                string token = Paths.SkillFamily.FalseSonBodySpecialFamily.variants[1].skillDef.skillDescriptionToken;
                LanguageAPI.Add(token, "<style=cIsDamage>Stunning.</style> Charge up a <style=cDeath>devastating laser</style>, dealing <style=cIsDamage>800%-2400% damage</style> in a blast on impact.");
            }

            if (FalseSonCoolerShards) {
                Paths.GameObject.LunarSpike.AddComponent<ProjectileTargetComponent>();
                var tf = Paths.GameObject.LunarSpike.AddComponent<ProjectileDirectionalTargetFinder>();
                var st = Paths.GameObject.LunarSpike.AddComponent<ProjectileSteerTowardTarget>();
                st.rotationSpeed = 30f;
                tf.lookRange = 40f;
                tf.lookCone = 7f;
                Paths.GameObject.LunarSpike.GetComponent<ProjectileSimple>().updateAfterFiring = true;

                FalseSonShardGhost = PrefabAPI.InstantiateClone(Paths.GameObject.LunarSpikeGhost, "FalseSonShardGhost");
                Paths.GameObject.LunarSpike.GetComponent<ProjectileController>().ghostPrefab = FalseSonShardGhost;
            }

            if (FalseSonLunarShotgun) {
                Paths.SkillDef.FalseSonBodyLunarSpikes.activationState = new(typeof(ChargeShards));
                Paths.SkillDef.FalseSonBodyLunarSpikes.stockToConsume = 0;

                LanguageAPI.Add(
                    Paths.SkillDef.FalseSonBodyLunarSpikes.skillDescriptionToken,
                    "<style=cIsUtility>Lunar Ruin.</style> Charge and fire up to <style=cIsUtility>6</style> Lunar Spikes for <style=cIsDamage>200% damage</style> each. Gain additional Lunar Spikes through <style=cIsHealing>Growth</style>."
                );
            }

            if (FalseSonGrowthScaling) {
                IL.RoR2.FalseSonController.GetTotalSpikeCount += AllowGrowthScaling;
            }

            if (FalseSonBetterSwing) {
                FalseSonSwingEffect = PrefabAPI.InstantiateClone(Paths.GameObject.FalseSonSwingBasic, "FalseSonSwingEffect");
                FalseSonSwingEffect2 = PrefabAPI.InstantiateClone(Paths.GameObject.FalseSonSwingBasic2, "FalseSonSwingEffect2");

                FalseSonSwingEffect.GetComponentInChildren<ParticleSystemRenderer>().transform.localScale = new(0.5f, 0.5f, 0.5f);
                FalseSonSwingEffect2.GetComponentInChildren<ParticleSystemRenderer>().transform.localScale = new(0.5f, 0.5f, 0.5f);

                On.EntityStates.FalseSon.ClubSwing.OnEnter += ClubSwing_OnEnter;
            }

            if (FalseSonAgility) {
                Paths.SkillFamily.FalseSonBodyPrimaryFamily.variants[0].skillDef.cancelSprintingOnActivation = false;
                Paths.SkillFamily.FalseSonBodyPrimaryFamily.variants[0].skillDef.canceledFromSprinting = false;
            }
        }

        private static void ClubSwing_OnEnter(On.EntityStates.FalseSon.ClubSwing.orig_OnEnter orig, ClubSwing self)
        {
            self.swingEffectPrefab = FalseSonSwingEffect;
            ClubSwing.secondarySwingEffectPrefab = FalseSonSwingEffect2;
            orig(self);
        }

        private static void AllowGrowthScaling(ILContext il)
        {
            ILCursor c = new(il);

            c.TryGotoNext(MoveType.Before, x => x.MatchStloc(3));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, FalseSonController, float>>((input, fsc) => {
                return input + ((fsc.characterBody.levelMaxHealth * 0.125f) * (fsc.characterBody.level - 1));
            });
        }

        private static void LaserFatherCharged_OnExit(On.EntityStates.FalseSon.LaserFatherCharged.orig_OnExit orig, LaserFatherCharged self)
        {
            orig(self);

            if (self.laserEffectEnd) {
                GameObject.Destroy(self.laserEffectEnd.gameObject);
            }
        }

        private static void LaserFatherCharged_FixedUpdate(On.EntityStates.FalseSon.LaserFatherCharged.orig_FixedUpdate orig, LaserFatherCharged self)
        {
            orig(self);

            if (self.laserEffectEnd && self.laserEffectEnd.transform.parent) {
                self.laserEffectEnd.transform.parent = null;
            }

            if (self.laserEffectEnd && !self.lockedOnHurtBox) {
                self.laserEffectEnd.transform.position = GetOutPos(self.GetAimRay());
            }
        }

        private static void LaserFatherCharged_OnEnter(On.EntityStates.FalseSon.LaserFatherCharged.orig_OnEnter orig, LaserFatherCharged self)
        {
            self.laserPrefab = Paths.GameObject.LunarGazeFireLaser;
            self.effectPrefab = Paths.GameObject.LunarGazeFireEffect;
            orig(self);
        }

        private static void LaserFather_FixedUpdate(On.EntityStates.FalseSon.LaserFather.orig_FixedUpdate orig, LaserFather self)
        {
            orig(self);
            if (self.laserLineComponent) {
                self.laserLineComponent.startWidth = 1f - self.charge;
                self.laserLineComponent.endWidth = 1f - self.charge;

                Color color = self.laserLineComponent.material.GetColor("_TintColor");
                self.laserLineComponent.material.SetColor("_TintColor", new Color(color.r, color.g, color.b, Mathf.Clamp01((1f - self.charge) * 1.75f)));

                self.laserLineComponent.SetPosition(0, self.laserLineComponent.transform.position);
                self.laserLineComponent.SetPosition(1, GetOutPos(self.GetAimRay()));
            }
        }

        public static Vector3 GetOutPos(Ray aim) {
            if (Physics.Raycast(aim, out RaycastHit hit, 2000f, LayerIndex.world.mask | LayerIndex.entityPrecise.mask)) {
                return hit.point;
            }

            return aim.GetPoint(400f);
        }

        private static void LaserFather_OnEnter(On.EntityStates.FalseSon.LaserFather.orig_OnEnter orig, LaserFather self)
        {
            LaserFather.chargeVfxPrefab = Paths.GameObject.LunarGazeChargeEffect;
            LaserFather.effectPrefab = Paths.GameObject.LunarGazeChargeEffect;
            LaserFather.laserPrefab = Paths.GameObject.LunarGazeChargeLaser;
            orig(self);
            self.chargeEffect.transform.localScale *= 0.5f;
        }
    }
}