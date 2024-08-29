using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Reflection;
using RoR2.UI;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets.ResourceLocators;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;

namespace StormTweaks {
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "ModAuthorName";
        public const string PluginName = "StormTweaks";
        public const string PluginVersion = "1.0.0";
        public static ConfigFile config;

        public static BepInEx.Logging.ManualLogSource ModLogger;

        public void Awake() {
            // set logger
            ModLogger = Logger;
            config = this.Config;
            
        }

        public static T Bind<T>(string sec, string name, string desc, T val) {
            return config.Bind<T>(sec, name, val, desc).Value;
        }
    }
}