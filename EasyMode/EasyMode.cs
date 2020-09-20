using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HutongGames.PlayMaker;
using Modding;
using UnityEngine;
using SeanprCore;
using HutongGames.PlayMaker.Actions;

namespace EasyMode
{
    public class EasyMode : Mod, ITogglableMod
    {
        public override void Initialize()
        {
            ModHooks.Instance.GetPlayerIntHook += GetInt;
            ModHooks.Instance.SetPlayerIntHook += SetInt;
            On.PlayerData.UpdateBlueHealth += BlueHealthHook;
            GameManager.instance.StartCoroutine(ToggleShadeSpawn(true));
            GameManager.instance.StartCoroutine(ToggleFastFocus(true));
        }

        public void Unload()
        {
            ModHooks.Instance.GetPlayerIntHook -= GetInt;
            ModHooks.Instance.SetPlayerIntHook -= SetInt;
            On.PlayerData.UpdateBlueHealth -= BlueHealthHook;
            GameManager.instance.StartCoroutine(ToggleShadeSpawn(false));
            GameManager.instance.StartCoroutine(ToggleFastFocus(false));
        }

        public override string GetVersion()
        {
            return "1.0";
        }

        private void BlueHealthHook(On.PlayerData.orig_UpdateBlueHealth orig, PlayerData self)
        {
            orig(self);

            // Free blue health from benches
            self.healthBlue += 2;
        }

        private int GetInt(string intName)
        {
            // Reduced charm cost
            if (intName.StartsWith("charmCost")) return PlayerData.instance.GetIntInternal(intName) - 1;

            // More damage
            if (intName == nameof(PlayerData.instance.nailDamage)) return 8 + 5 * PlayerData.instance.nailSmithUpgrades;

            return PlayerData.instance.GetIntInternal(intName);
        }

        private void SetInt(string intName, int value)
        {
            // More soul
            if (intName == nameof(PlayerData.MPCharge) && value > PlayerData.instance.MPCharge) value = Math.Min(value + 6, 100);

            PlayerData.instance.SetIntInternal(intName, value);
        }

        private IEnumerator ToggleShadeSpawn(bool newSetting)
        {
            yield return null;
            yield return new WaitWhile(() => HeroController.instance == null || HeroController.instance.heroDeathPrefab == null);
            try
            {
                PlayMakerFSM fsm = HeroController.instance.heroDeathPrefab.LocateMyFSM("Hero Death Anim");
                FsmState MapZone = fsm.FsmStates.First(state => state.Name == "Map Zone");
                FsmState WPCheck = fsm.FsmStates.First(state => state.Name == "WP Check");
                if (newSetting)
                {
                    Log("Shade spawn disabled");
                    MapZone.Transitions.First(t => t.EventName == "FINISHED").ToState = "Anim Start";
                    try
                    {
                        FsmState WPCheck2 = fsm.FsmStates.First(state => state.Name == "WP Check2");
                    }
                    catch
                    {
                        FsmState WPCheck2 = new FsmState(WPCheck)
                        {
                            Name = "WP Check2"
                        };
                        List<FsmState> list = fsm.FsmStates.ToList<FsmState>();
                        list.Add(WPCheck2);
                        fsm.Fsm.States = list.ToArray();
                        StringCompare inGH = WPCheck.Actions.Last() as StringCompare;
                        StringCompare sc = new StringCompare
                        {
                            stringVariable = inGH.stringVariable,
                            compareTo = "GODS_GLORY",
                            equalEvent = inGH.notEqualEvent,
                            notEqualEvent = inGH.equalEvent,
                            storeResult = false,
                            everyFrame = false
                        };
                        inGH = sc;
                    }
                    WPCheck.Transitions.First(t => t.EventName == "WHITE PALACE").ToState = "WP Check2";
                    // exit the death sequence through the white palace path
                    if (WPCheck.Actions.Last() is StringCompare inWP)
                    {
                        WPCheck.AddFirstAction(new StringCompare
                        {
                            stringVariable = inWP.stringVariable,
                            compareTo = "DREAM_WORLD",
                            equalEvent = inWP.notEqualEvent,
                            notEqualEvent = inWP.equalEvent,
                            storeResult = false,
                            everyFrame = false
                        });
                    }
                }
                else
                {
                    Log("Shade spawn reenabled");
                    MapZone.Transitions.First(t => t.EventName == "FINISHED").ToState = "Break Glass HP";
                    WPCheck.Transitions.First(t => t.EventName == "WHITE PALACE").ToState = "Wait for HeroController";
                    WPCheck.Actions = new FsmStateAction[] { WPCheck.Actions.Last() };
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        private IEnumerator ToggleFastFocus(bool newSetting)
        {
            yield return null;
            yield return new WaitWhile(() => HeroController.instance == null || HeroController.instance.spellControl == null);
            try
            {
                FsmState DeepFocusSpeed = HeroController.instance.spellControl.GetState("Deep Focus Speed");
                if (newSetting)
                {
                    Log("Decreasing focus speed");
                    if (DeepFocusSpeed.Actions.Last() is FloatMultiply slowFocus)
                    {
                        DeepFocusSpeed.AddFirstAction(new FloatMultiply
                        {
                            floatVariable = slowFocus.floatVariable,
                            multiplyBy = 0.5f,
                            everyFrame = false
                        });
                    }
                }
                else
                {
                    Log("Returning focus speed to normal");
                    DeepFocusSpeed.Actions = new FsmStateAction[]
                    {
                    DeepFocusSpeed.Actions[1], DeepFocusSpeed.Actions[2]
                    };
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }
    }
}
