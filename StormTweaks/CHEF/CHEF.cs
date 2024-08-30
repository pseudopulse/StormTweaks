using System;
using Mono.Cecil;
using EntityStates.Chef;
using JetBrains.Annotations;
using MonoMod.Cil;
using Rewired;
using ThreeEyedGames;
using Mono.Cecil.Cil;

namespace StormTweaks {
    public class CHEF {
        public static float ChefDiceDuration => Bind<float>("CHEF: Dice", "Attack Duration", "The time it takes before another knife becomes throwable.", 0.3f);
        public static bool ChefDiceEnabled => Bind<bool>("CHEF: Dice", "Enabled", "Enable changes to this skill?", true);
        public static bool ChefRename => Bind<bool>("CHEF: General", "Capitalization Change", "Renames Chef to CHEF to be in-line with his previous iterations.", true);
        public static bool ChefSpecialRename => Bind<bool>("CHEF: Yes Chef", "Name Reversion", "Renames Yes, Chef back to Second Helping, the way it is in RoR1 and RoRR.", true);
        public static float ChefSpecialCooldown => Bind<float>("CHEF: Yes Chef", "Cooldown", "Lower the cooldown of the alternate special.", 8f);
        public static bool ChefInterruptableSkills => Bind<bool>("CHEF: General", "Skill Interrupts", "Make CHEF skills be able to interrupt each other.", true);
        public static bool ChefRollSpeed => Bind<bool>("CHEF: Roll", "Charge-Based Speed", "Increase the speed of Roll when charged.", true);
        public static bool ChefRollOil => Bind<bool>("CHEF: Roll", "Oil Trail", "Should Roll leave a trail of oil when boosted by Second Helping?", true);
        public static float ChefSearDistance => Bind<float>("CHEF: Sear", "Max Distance", "The distance Sear should damage targets.", 22);
        public static bool ChefSearNoDirLock => Bind<bool>("CHEF: Sear", "No Direction Lock", "Make Sear remain omnidirectional even during sprint.", true);
        private static CleaverSkillDef DiceStandard;
        private static GameObject OilTrailSegment;
        private static GameObject OilTrailSegmentGhost;
        public static DamageAPI.ModdedDamageType GlazeOnHit = DamageAPI.ReserveDamageType();
        public static void Init() {
            SurvivorDef CHEF = Paths.SurvivorDef.Chef;
            if (ChefRename) LanguageAPI.Add(CHEF.displayNameToken, "CHEF");
            if (ChefSpecialRename) LanguageAPI.Add(Paths.SkillDef.YesChef.skillNameToken, "Second Helping");

            Paths.SkillDef.YesChef.baseRechargeInterval = ChefSpecialCooldown;

            if (ChefDiceEnabled) {
                On.EntityStates.Chef.Dice.FixedUpdate += Dice_FixedUpdate;
                On.ChefController.Update += ChefController_Update;
                On.EntityStates.Chef.Dice.OnExit += Dice_OnExit;
                On.EntityStates.Chef.Dice.OnEnter += Dice_OnEnter;
                On.RoR2.Projectile.CleaverProjectile.Start += CleaverProjectile_Start;
                On.RoR2.Projectile.CleaverProjectile.OnDestroy += CleaverProjectile_OnDestroy;

                DiceStandard = ScriptableObject.CreateInstance<CleaverSkillDef>();
                DiceStandard.Clone(Paths.SkillDef.ChefDice);
                DiceStandard.mustKeyPress = false;
                DiceStandard.rechargeStock = 0;
                DiceStandard.stockToConsume = 0;

                ContentAddition.AddSkillDef(DiceStandard);

                Paths.SkillFamily.ChefPrimaryFamily.variants[0].skillDef = DiceStandard;

                Paths.GameObject.ChefBody.AddComponent<ChefCleaverStorage>();

                LanguageAPI.Add(Paths.SkillDef.ChefDice.skillDescriptionToken, 
                    "Throw up to <style=cIsUtility>3</style> cleavers for <style=cIsDamage>250% damage</style>. Release to recall the cleavers, dealing <style=cIsDamage>375% damage</style> on the return trip."
                );
            }

            if (ChefInterruptableSkills) {
                Paths.SkillDef.ChefSear.interruptPriority = InterruptPriority.PrioritySkill;
                Paths.SkillDef.ChefSearBoosted.interruptPriority = InterruptPriority.Frozen;
                Paths.SkillDef.ChefRolyPoly.interruptPriority = InterruptPriority.PrioritySkill;
                Paths.SkillDef.ChefRolyPolyBoosted.interruptPriority = InterruptPriority.Frozen;
                Paths.SkillDef.ChefGlaze.interruptPriority = InterruptPriority.PrioritySkill;

                On.EntityStates.Chef.Sear.GetMinimumInterruptPriority += Sear_GetMinimumInterruptPriority;
                On.EntityStates.Chef.RolyPoly.GetMinimumInterruptPriority += RolyPoly_GetMinimumInterruptPriority;
                On.EntityStates.Chef.Glaze.GetMinimumInterruptPriority += Glaze_GetMinimumInterruptPriority;
                On.EntityStates.Chef.ChargeRolyPoly.GetMinimumInterruptPriority += ChargeGlaze_GetMinimumInterruptPriority;
            }

            if (ChefRollOil) {
                OilTrailSegment = PrefabAPI.InstantiateClone(Paths.GameObject.CrocoLeapAcid, "OilTrailSegment");
                OilTrailSegmentGhost = PrefabAPI.InstantiateClone(Paths.GameObject.CrocoLeapAcidGhost, "OilTrailSegmentGhost");

                
                OilTrailSegmentGhost.FindComponent<Decal>("Decal").Material = Paths.Material.matClayBossGooDecal;
                OilTrailSegmentGhost.FindParticle("Spittle").gameObject.SetActive(false);
                OilTrailSegmentGhost.FindParticle("Gas").gameObject.SetActive(false);
                OilTrailSegmentGhost.FindComponent<Light>("Point Light").gameObject.SetActive(false);
                OilTrailSegmentGhost.GetComponent<ProjectileGhostController>().inheritScaleFromProjectile = true;


                OilTrailSegment.GetComponent<ProjectileDamage>().damageType = DamageType.ClayGoo | DamageType.Silent;
                OilTrailSegment.transform.localScale *= 0.5f;

                OilTrailSegment.GetComponent<ProjectileController>().ghostPrefab = OilTrailSegmentGhost;
                OilTrailSegment.GetComponent<ProjectileDotZone>().damageCoefficient = 0f;
                OilTrailSegment.GetComponent<ProjectileDotZone>().impactEffect = null;
                OilTrailSegment.GetComponent<ProjectileDotZone>().fireFrequency = 20;
                OilTrailSegment.GetComponent<ProjectileDotZone>().overlapProcCoefficient = 0;
                OilTrailSegment.RemoveComponents<AkEvent>();
                OilTrailSegment.RemoveComponent<AkGameObj>();
                OilTrailSegment.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>().Add(GlazeOnHit);

                GlobalEventManager.onServerDamageDealt += OnDamageDealt;
    
                ContentAddition.AddProjectile(OilTrailSegment);
                Paths.GameObject.ChefBody.AddComponent<ChefTrailBehaviour>();
            }

            if (ChefSearNoDirLock) {
                On.EntityStates.Chef.Sear.Update += Sear_Update;
                IL.EntityStates.Chef.Sear.FirePrimaryAttack += Sear_FirePrimaryAttack;
            }

            On.EntityStates.Chef.Sear.OnEnter += Sear_OnEnter;
        }

        private static void Sear_OnEnter(On.EntityStates.Chef.Sear.orig_OnEnter orig, Sear self)
        {
            Sear.maxDistance = ChefSearDistance;
            orig(self);
        }

        private static void Sear_FirePrimaryAttack(ILContext il)
        {
            ILCursor c = new(il);

            c.TryGotoNext(MoveType.Before, 
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchDup(),
                x => x.MatchLdcR4(0)
            );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Vector3, Sear, Vector3>>((inVec, instance) => {
                return instance.GetAimRay().direction;
            });
        }

        private static void Sear_Update(On.EntityStates.Chef.Sear.orig_Update orig, Sear self)
        {
            orig(self);

            if (self.flamethrowerEffectInstance) {
                self.flamethrowerEffectInstance.forward = self.GetAimRay().direction;
            }
        }

        private static void OnDamageDealt(DamageReport report)
        {
            if (report.damageInfo.HasModdedDamageType(GlazeOnHit) && report.victimBody) {
                report.victimBody.AddTimedBuff(DLC2Content.Buffs.CookingOiled, 3f, 1);
            }
        }

        public class ChefTrailBehaviour : MonoBehaviour {
            public ChefController chef;
            public Timer timer = new(0.1f, false, true, false, true);
            public void Start() {
                chef = GetComponent<ChefController>();
            }
            public void FixedUpdate() {
                if (chef.rolyPolyActive && chef.yesChefHeatActive && chef.hasAuthority) {
                    if (timer.Tick()) {
                        FireProjectileInfo info = new();
                        info.damage = chef.characterBody.damage;
                        info.crit = false;
                        info.rotation = Quaternion.identity;
                        info.position = chef.characterBody.footPosition;
                        info.projectilePrefab = OilTrailSegment;
                        info.owner = base.gameObject;
                        
                        ProjectileManager.instance.FireProjectile(info);
                    }
                }
            }
        }

        private static InterruptPriority RolyPoly_GetMinimumInterruptPriority(On.EntityStates.Chef.RolyPoly.orig_GetMinimumInterruptPriority orig, RolyPoly self)
        {
            return InterruptPriority.PrioritySkill;
        }

        private static InterruptPriority Glaze_GetMinimumInterruptPriority(On.EntityStates.Chef.Glaze.orig_GetMinimumInterruptPriority orig, Glaze self)
        {
            return InterruptPriority.PrioritySkill;
        }

        private static InterruptPriority ChargeGlaze_GetMinimumInterruptPriority(On.EntityStates.Chef.ChargeRolyPoly.orig_GetMinimumInterruptPriority orig, ChargeRolyPoly self)
        {
            return InterruptPriority.PrioritySkill;
        }

        private static InterruptPriority Sear_GetMinimumInterruptPriority(On.EntityStates.Chef.Sear.orig_GetMinimumInterruptPriority orig, Sear self)
        {
            return InterruptPriority.PrioritySkill;
        }

        private static void Dice_OnEnter(On.EntityStates.Chef.Dice.orig_OnEnter orig, Dice self)
        {
            orig(self);

            ChefCleaverStorage.Reset(self.characterBody, self.chefController.yesChefHeatActive ? 0.8f : 0.4f);
        }

        private static void CleaverProjectile_OnDestroy(On.RoR2.Projectile.CleaverProjectile.orig_OnDestroy orig, CleaverProjectile self)
        {
            orig(self);

            if (self.chefController) {
                CharacterBody body = self.chefController.characterBody;
                if (ChefCleaverStorage.cleaverMap.ContainsKey(body)) {
                    if (!ChefCleaverStorage.cleaverMap[body].Contains(self)) return;
                    ChefCleaverStorage.cleaverMap[body].Remove(self);
                }
            }
        }

        private static void CleaverProjectile_Start(On.RoR2.Projectile.CleaverProjectile.orig_Start orig, CleaverProjectile self)
        {
            orig(self);

            if (self.chefController) {
                CharacterBody body = self.chefController.characterBody;
                if (ChefCleaverStorage.cleaverMap.ContainsKey(body)) {
                    ChefCleaverStorage.cleaverMap[body].Add(self);
                }
            }
        }

        private static void Dice_OnExit(On.EntityStates.Chef.Dice.orig_OnExit orig, Dice self)
        {
            self.chefController.SetYesChefHeatState(false);

            if (NetworkServer.active) {
                self.characterBody.RemoveBuff(DLC2Content.Buffs.boostedFireEffect);
            }

            if (self.isAuthority) {
                self.chefController.ClearSkillOverrides();
            }

            self.PlayAnimation("Gesture, Override", "DiceReturnCatch", "DiceReturnCatch.playbackRate", self.duration);
		    self.PlayAnimation("Gesture, Additive", "DiceReturnCatch", "DiceReturnCatch.playbackRate", self.duration);
        }
        private static void ChefController_Update(On.ChefController.orig_Update orig, ChefController self)
        {
            orig(self);

            self.recallCleaver = self.cleaverAway && !self.characterBody.inputBank.skill1.down && ChefCleaverStorage.GetCanBodyRecall(self.characterBody);
        }

        private static void Dice_FixedUpdate(On.EntityStates.Chef.Dice.orig_FixedUpdate orig, Dice self)
        {
            self.recallInputPressed = false;
            self.recallBackupCountdown = 900f;
            orig(self);

            if (self.fixedAge >= ChefDiceDuration / self.attackSpeedStat) {
                self.outer.SetNextStateToMain();
            }
        }

        public class ChefCleaverStorage : MonoBehaviour {
            public static Dictionary<CharacterBody, List<CleaverProjectile>> cleaverMap = new();
            public static Dictionary<CharacterBody, ChefCleaverStorage> storageMap = new();
            private CharacterBody body;
            public float timer = 0f;

            public void OnEnable() {
                body = GetComponent<CharacterBody>();
                cleaverMap.Add(body, new());
                storageMap.Add(body, this);
            }

            public void FixedUpdate() {
                if (timer >= 0f) {
                    timer -= Time.fixedDeltaTime;
                }
            }

            public static bool GetCanBodyRecall(CharacterBody body) {
                if (storageMap.ContainsKey(body)) {
                    return storageMap[body].timer <= 0f;
                }

                return true;
            }

            public static void Reset(CharacterBody body, float time = 0.4f) {
                if (storageMap.ContainsKey(body)) {
                    storageMap[body].timer = time;
                }
            }

            public void OnDestroy() {
                if (body && cleaverMap.ContainsKey(body)) {
                    cleaverMap.Remove(body);
                }

                storageMap.Remove(body);
            }

            public int GetCleaversActive() {
                if (body) {
                    return cleaverMap[body].Count;
                }

                return 0;
            }
        }

        public class CleaverSkillDef : SkillDef {
            public class CleaverInstanceData : BaseSkillInstanceData {
                public ChefCleaverStorage controller;
            }
            public override BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
            {
                return new CleaverInstanceData {
                    controller = skillSlot.GetComponent<ChefCleaverStorage>()
                };
            }
            public override void OnFixedUpdate([NotNull] GenericSkill skillSlot, float deltaTime)
            {
                base.OnFixedUpdate(skillSlot, deltaTime);
                int clcount = (skillSlot.skillInstanceData as CleaverInstanceData).controller.GetCleaversActive();
                int count = skillSlot.maxStock - clcount;
                skillSlot.stock = Mathf.Clamp(count, 0, skillSlot.maxStock);
            }

            public void Clone(SkillDef from) {
                this.activationState = from.activationState;
                this.activationStateMachineName = from.activationStateMachineName;
                this.attackSpeedBuffsRestockSpeed = from.attackSpeedBuffsRestockSpeed;
                this.attackSpeedBuffsRestockSpeed_Multiplier = from.attackSpeedBuffsRestockSpeed_Multiplier;
                this.autoHandleLuminousShot = from.autoHandleLuminousShot;
                this.baseMaxStock = from.baseMaxStock;
                this.baseRechargeInterval = from.baseRechargeInterval;
                this.beginSkillCooldownOnSkillEnd = from.beginSkillCooldownOnSkillEnd;
                this.canceledFromSprinting = from.canceledFromSprinting;
                this.cancelSprintingOnActivation = from.cancelSprintingOnActivation;
                this.dontAllowPastMaxStocks = from.dontAllowPastMaxStocks;
                this.forceSprintDuringState = from.forceSprintDuringState;
                this.fullRestockOnAssign = from.fullRestockOnAssign;
                this.hideStockCount = from.hideStockCount;
                this.icon = from.icon;
                this.interruptPriority = from.interruptPriority;
                this.isCombatSkill = from.isCombatSkill;
                this.keywordTokens = from.keywordTokens;
                this.mustKeyPress = from.mustKeyPress;
                this.rechargeStock = from.rechargeStock;
                this.requiredStock = from.requiredStock;
                this.resetCooldownTimerOnUse = from.resetCooldownTimerOnUse;
                this.skillDescriptionToken = from.skillDescriptionToken;
                this.skillName = from.skillName;
                this.skillNameToken = from.skillNameToken;
                this.stockToConsume = from.stockToConsume;
            }
        }
    }
}