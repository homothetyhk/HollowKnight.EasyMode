using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HutongGames.PlayMaker;
using Modding;
using UnityEngine;
using HutongGames.PlayMaker.Actions;

namespace EasyMode
{
    public class EasyMode : Mod, ITogglableMod, IGlobalSettings<Settings>, IMenuMod
    {
        public static Settings Settings = new Settings();
        private static EasyMode instance;
        public static bool Enabled = false;

        public class CoroutineHolder : MonoBehaviour { }
        private CoroutineHolder coroutineHolder;

        public override void Initialize()
        {
            instance = this;
            Enabled = true;
            coroutineHolder = new GameObject("EasyMode Coroutine Holder").AddComponent<CoroutineHolder>();
            UnityEngine.Object.DontDestroyOnLoad(coroutineHolder);

            ModHooks.GetPlayerIntHook += GetInt;
            ModHooks.SetPlayerIntHook += SetInt;
            On.PlayerData.UpdateBlueHealth += BlueHealthHook;
            On.HeroController.Start += OnStart;
            if (HeroController.instance != null)
            {
                ToggleShadeSpawn();
                ToggleFastFocus();
            }
        }

        private void OnStart(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);
            ToggleShadeSpawn();
            ToggleFastFocus();
        }

        public void Unload()
        {
            ModHooks.GetPlayerIntHook -= GetInt;
            ModHooks.SetPlayerIntHook -= SetInt;
            On.PlayerData.UpdateBlueHealth -= BlueHealthHook;
            On.HeroController.Start -= OnStart;
            if (HeroController.instance != null)
            {
                ToggleShadeSpawn();
                ToggleFastFocus();
            }
            Enabled = false;
        }

        public override string GetVersion()
        {
            return "1.1";
        }

        private void BlueHealthHook(On.PlayerData.orig_UpdateBlueHealth orig, PlayerData self)
        {
            orig(self);

            // Free blue health from benches
            if (Settings.ExtraBlueHP) self.healthBlue += Settings.ExtraBlueHPAmount;
        }

        private int GetInt(string intName, int value)
        {
            // Reduced charm cost
            if (Settings.ReducedCharmCost && intName.StartsWith("charmCost")) return value - 1;

            // More damage
            if (Settings.ExtraNailDamage && intName == nameof(PlayerData.instance.nailDamage))
            {
                return Settings.ExtraNailDamageBase + Settings.ExtraNailDamagePerUpgrade * (value - 5) / 4;
            }

            return value;
        }

        private int SetInt(string intName, int value)
        {
            // More soul
            if (Settings.ExtraSoulRecharge && intName == nameof(PlayerData.MPCharge) && value > PlayerData.instance.MPCharge) return Math.Min(value + Settings.ExtraSoulPerHitAmount, 100);

            return value;
        }

        public static void ToggleShadeSpawn()
        {
            if (instance == null || instance.coroutineHolder == null || !Enabled) return;
            instance.coroutineHolder.StartCoroutine(instance.ToggleShadeSpawnCoroutine());
        }

        private IEnumerator ToggleShadeSpawnCoroutine()
        {
            yield return null;
            yield return new WaitWhile(() => HeroController.instance == null || HeroController.instance.heroDeathPrefab == null);
            try
            {
                PlayMakerFSM fsm = HeroController.instance.heroDeathPrefab.LocateMyFSM("Hero Death Anim");
                FsmState mapZone = fsm.GetState("Map Zone");
                FsmState wpCheck = fsm.GetState("WP Check");
                FsmString zone = fsm.FsmVariables.FindFsmString("Map Zone");

                if (Settings.NoShadeOnDeath)
                {
                    FsmState animStart = fsm.GetState("Anim Start");
                    mapZone.Transitions.First(t => t.EventName == "FINISHED").SetToState(animStart);

                    wpCheck.Actions = new FsmStateAction[]
                    {
                        new StringCompare
                        {
                            stringVariable = zone,
                            compareTo = "DREAM_WORLD",
                            equalEvent = FsmEvent.Finished,
                            notEqualEvent = null,
                            everyFrame = false,
                            storeResult = false,
                        },
                        new StringCompare
                        {
                            stringVariable = zone,
                            compareTo = "GODS_GLORY",
                            equalEvent = FsmEvent.Finished,
                            notEqualEvent = FsmEvent.GetFsmEvent("WHITE PALACE"),
                            everyFrame = false,
                            storeResult = false,
                        },
                    };
                }
                else
                {
                    FsmState breakGlassHP = fsm.GetState("Break Glass HP");
                    mapZone.Transitions.First(t => t.EventName == "FINISHED").SetToState(breakGlassHP);

                    wpCheck.Actions = new FsmStateAction[]
                    {
                        new StringCompare
                        {
                            stringVariable = zone,
                            compareTo = "WHITE_PALACE",
                            equalEvent = FsmEvent.GetFsmEvent("WHITE PALACE"),
                            notEqualEvent = null,
                            everyFrame = false,
                            storeResult = false,
                        },
                    };
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        public static void ToggleFastFocus()
        {
            if (instance == null || instance.coroutineHolder == null || !Enabled) return;
            instance.coroutineHolder.StartCoroutine(instance.ToggleFastFocusCoroutine());
        }

        private IEnumerator ToggleFastFocusCoroutine()
        {
            yield return null;
            yield return new WaitWhile(() => HeroController.instance == null || HeroController.instance.spellControl == null);
            try
            {
                FsmState deepFocusSpeed = HeroController.instance.spellControl.GetState("Deep Focus Speed");
                FsmFloat tpd = HeroController.instance.spellControl.FsmVariables.FindFsmFloat("Time Per MP Drain");
                PlayerDataBoolTest test = deepFocusSpeed.GetFirstActionOfType<PlayerDataBoolTest>();
                FloatMultiply slowFocus = deepFocusSpeed.GetLastActionOfType<FloatMultiply>();
                if (test == null || slowFocus == null) throw new InvalidOperationException("Unable to find Deep Focus Speed actions.");

                if (Settings.FasterFocus)
                {
                    deepFocusSpeed.Actions = new FsmStateAction[]
                    {
                        new FloatMultiply
                        {
                            floatVariable = tpd,
                            multiplyBy = Settings.FasterFocusTimeMultipler,
                            everyFrame = false,
                        },
                        test,
                        slowFocus,
                    };
                }
                else
                {
                    deepFocusSpeed.Actions = new FsmStateAction[] { test, slowFocus };
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        void IGlobalSettings<Settings>.OnLoadGlobal(Settings s)
        {
            Settings = s ?? Settings ?? new Settings();
        }

        Settings IGlobalSettings<Settings>.OnSaveGlobal()
        {
            return Settings;
        }

        bool IMenuMod.ToggleButtonInsideMenu => true;
        List<IMenuMod.MenuEntry> IMenuMod.GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            List<IMenuMod.MenuEntry> entries = new List<IMenuMod.MenuEntry>();
            entries.Add(toggleButtonEntry ?? throw new ArgumentNullException(nameof(toggleButtonEntry)));

            string[] bools = new string[] { "Off", "On" };
            entries.AddRange(Settings.GetType().GetProperties()
                .Where(f => f.PropertyType == typeof(bool))
                .Select(f => new IMenuMod.MenuEntry(
                f.Name.FromCamelCase(),
                bools,
                string.Empty,
                i => f.SetValue(Settings, i == 1),
                () => ((bool)f.GetValue(Settings)) ? 1 : 0
            )));

            return entries;
        }
    }
}
