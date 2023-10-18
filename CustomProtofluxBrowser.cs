using System;
using System.Collections.Generic;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ResoniteModLoader;
using SpecialItemsLib;

namespace CustomProtofluxBrowser
{
    public class CustomProtofluxBrowser : ResoniteMod
    {
        public override string Name => "CustomProtofluxBrowser";
        public override string Author => "AlexW-578";
        public override string Version => "2.1.1";
        public override string Link => "https://github.com/AlexW-578/CustomProtofluxBrowser";

        private static ModConfiguration Config;

        [AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> Enabled = new ModConfigurationKey<bool>("Enabled", "Enables the mod", () => true);
        [AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> EnableModifier = new ModConfigurationKey<bool>("Enable Modifier", "Apply Custom Scale Modifiers", () => false);
        [AutoRegisterConfigKey] private static readonly ModConfigurationKey<float> ScaleModifier = new ModConfigurationKey<float>("Scale Modifier", "Browser size relative to the original scale", () => 1f);
        private static string PROTOFLUX_BROWSER_TAG
        {
            get { return "custom_protoflux_browser"; }
        }

        private static CustomSpecialItem protofluxBrowserObject;

        public override void OnEngineInit()
        {
            Config = GetConfiguration();
            Config.Save(true);
            Harmony harmony = new Harmony("co.uk.alexw-578.CustomProtofluxBrowser");
            protofluxBrowserObject = SpecialItemsLib.SpecialItemsLib.RegisterItem(PROTOFLUX_BROWSER_TAG, "Protoflux Browser");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(SlotHelper), nameof(SlotHelper.GenerateTags), new Type[] { typeof(Slot), typeof(HashSet<string>) })]
        class SlotHelper_GenerateTags_Patch
        {
            static MethodInfo NodeTypeSelectedMethod = AccessTools.Method(typeof(ProtoFluxTool), "OnNodeTypeSelected");

            static void Postfix(Slot slot, HashSet<string> tags)
            {
                var comp = slot.GetComponent<ComponentSelector>();
                if (comp != null && comp.ComponentSelected.Target?.Method == NodeTypeSelectedMethod)
                {
                    tags.Add(PROTOFLUX_BROWSER_TAG);
                }
            }
        }

        [HarmonyPatch(typeof(ProtoFluxTool), "OpenNodeBrowser")]
        class ResoniteProtofluxBrowser_Patch
        {
            static bool Prefix(ProtoFluxTool __instance)
            {
                if (!Config.GetValue(Enabled)) return true;

                if (protofluxBrowserObject.Uri == null) return true;

                Slot slot = __instance.LocalUserSpace.AddSlot("NodeMenu");
                slot.StartTask(async delegate ()
                {
                    await slot.LoadObjectAsync(protofluxBrowserObject.Uri);
                    InventoryItem component = slot.GetComponent<InventoryItem>();
                    Slot slot_two = ((component != null) ? component.Unpack() : null) ?? slot;
                    slot_two.PositionInFrontOfUser(float3.Backward,scale:false);
                    if (Config.GetValue(EnableModifier))
                    {
                        slot_two.GlobalScale *= Config.GetValue(ScaleModifier);
                    }
                });

                __instance.ActiveHandler?.CloseContextMenu();
                return false;
            }
        }
    }
}
