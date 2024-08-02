using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Kerbalism.External;
using Kerbalism.Modules;
using UnityEngine;

namespace Kerbalism.System
{
    public static class MM40Injector
    {
        private static List<string> injectors = new List<string>();

        public static void AddInjector(string type, string id)
        {
            injectors.Add(type + id);
        }

        public static IEnumerable<string> ModuleManagerAddToModList()
        {
            return injectors;
        }
    }

    // the name is chosen so that the awake method is called after ModuleManager,
    // this is necessary because MM injects its loader at index 1, so we need to inject
    // our own after it, at index 1 (so that it runs just before MM)
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public sealed class Loader : MonoBehaviour
    {
        public static Harmony HarmonyInstance => harmonyInstance;
        private static Harmony harmonyInstance;

        public void Start()
        {
            // log version
            Lib.Log("Version : " + Lib.KerbalismVersion + " - Build : " + Lib.KerbalismDevBuild);

            if (LocalHelpers.GenerateEnglishLoc)
                LocalHelpers.GenerateLoc();

            if (LocalHelpers.UpdateNonEnglishLoc)
                LocalHelpers.RegenerateNonEnglishLoc();

            Lib.Log("Forcing KSP to load resources...");
            PartResourceLibrary.Instance.LoadDefinitions();

            // parse settings
            Settings.Parse();
            // parse profile
            Profile.Profile.Parse();
            // detect features
            Features.Detect();

            // get configs from DB
            UrlDir.UrlFile root = null;
            foreach (UrlDir.UrlConfig url in GameDatabase.Instance.root.AllConfigs)
            {
                root = url.parent;
                break;
            }

            // inject MM patches on-the-fly, so that profile/features can be queried with NEEDS[]
            Inject(root, "Profile", Lib.UppercaseFirst(Settings.Profile));
            if (Features.Automation) Inject(root, "Feature", "Automation");
            if (Features.Science) Inject(root, "Feature", "Science");
            if (Features.Radiation) Inject(root, "Feature", "Radiation");

            // inject harmony patches
            harmonyInstance = new Harmony("Kerbalism");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            KerbalismSentinel.ApplyHarmonyPatches(harmonyInstance);
        }

        // inject an MM patch on-the-fly, so that NEEDS[TypeId] can be used in MM patches
        static void Inject(UrlDir.UrlFile root, string type, string id)
        {
            Lib.Log(Lib.BuildString("Injecting ", type, id));
            if (ModuleManager.mmMajor >= 4)
            {
                MM40Injector.AddInjector(type, id);
            }
            else
            {
                root.configs.Add(new UrlDir.UrlConfig(root,
                    new ConfigNode(Lib.BuildString("@Kerbalism:FOR[", type, id, "]"))));
            }
        }

        public static void ModuleManagerPostLoad()
        {
            Lib.LoadResourceUnitInfo();
        }
    }
} // KERBALISM
