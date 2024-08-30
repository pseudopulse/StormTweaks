namespace StormTweaks {
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "pseudopulse";
        public const string PluginName = "StormTweaks";
        public const string PluginVersion = "1.0.0";
        public static ConfigFile config;

        public static BepInEx.Logging.ManualLogSource ModLogger;

        public void Awake() {
            // set logger
            ModLogger = Logger;
            config = this.Config;
            
            CHEF.Init();
            FalseSon.Init();
            Seeker.Init();
        }

        public static T Bind<T>(string sec, string name, string desc, T val) {
            return config.Bind<T>(sec, name, val, desc).Value;
        }
    }
}