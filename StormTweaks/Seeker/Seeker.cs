using System;

namespace StormTweaks {
    public class Seeker {
        public static float SeekerMeditateDamage => Bind<float>("Seeker: Meditation", "Explosion Damage", "Damage coefficient of Seeker's explosion. Vanilla is 4.5.", 8f);
        public static void Init() {
            On.EntityStates.Seeker.MeditationUI.OnEnter += OnEnter;
        }

        private static void OnEnter(On.EntityStates.Seeker.MeditationUI.orig_OnEnter orig, EntityStates.Seeker.MeditationUI self)
        {
            orig(self);
            self.damageCoefficient = SeekerMeditateDamage;
        }
    }
}