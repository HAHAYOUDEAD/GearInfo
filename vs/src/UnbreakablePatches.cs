using Il2Cpp;
using static Il2Cpp.FPSLogger;

namespace GearInfo
{
    internal class UnbreakablePatches
    {
        [HarmonyPatch(typeof(Panel_Inventory), nameof(Panel_Inventory.Enable), new Type[] { typeof(bool), typeof(bool) })]
        private static class InventoryPanelTracker
        {
            public static bool startTracking;

            internal static void Postfix(ref Panel_Inventory __instance, ref bool enable)
            {
                startTracking = enable;

                if (enable)
                {
                    // instantiate and hide UI
                    Control.SetupUI();
                    Control.GIUIRoot.active = false;
                    Control.ToggleWindow(false);
                }
                else
                {
                    // hide UI
                    UIInjection.currentlySelected = null;
                    Control.GIUIRoot.active = false;
                }
            }
        }

        [HarmonyPatch(typeof(Panel_Inventory), nameof(Panel_Inventory.Update))] 
        private static class UIInjection
        {
            public static GearItem? currentlySelected;

            internal static void Postfix(ref Panel_Inventory __instance)
            {
                if (!InventoryPanelTracker.startTracking) return;

                if (__instance.GetCurrentlySelectedItem()?.m_GearItem == currentlySelected) return;

                currentlySelected = __instance.GetCurrentlySelectedItem()?.m_GearItem;

                if (currentlySelected != null)
                {
                    if (Control.IsWindowEnabled())
                    {
                        Control.AdjustUIPosition(false, true);
                        Control.SetupRelevantData(currentlySelected);
                    }
                    // show UI
                    Control.GIUIRoot.active = true;
                    Control.CalculateMainButtonPosition();
                }
                else
                {
                    // hide UI
                    Control.ToggleWindow(false);
                    Control.GIUIRoot.active = false;
                }
            }
        }
        //[HarmonyPatch(typeof(Panel_Inventory), nameof(Panel_Inventory.Update))] 
        //private static class UIInjection
        //{
        //    public static GearItem? currentlySelected;

        //    internal static void Postfix(ref Panel_Inventory __instance)
        //    {
        //        if (!InventoryPanelTracker.startTracking) return;

        //        if (__instance.GetCurrentlySelectedItem()?.m_GearItem == currentlySelected) return;

        //        currentlySelected = __instance.GetCurrentlySelectedItem()?.m_GearItem;

        //        if (currentlySelected != null)
        //        {
        //            if (Control.IsWindowEnabled())
        //            {
        //                Control.AdjustUIPosition(false, true);
        //                Control.SetupRelevantData(currentlySelected);
        //            }
        //            // show UI
        //            Control.GIUIRoot.active = true;
        //            Control.CalculateButtonPosition();
        //        }
        //        else
        //        {
        //            // hide UI
        //            Control.ToggleWindow(false);
        //            Control.GIUIRoot.active = false;
        //        }
        //    }
        //}
    }
}
 