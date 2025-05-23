global using static GearInfo.Utility;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LocalizationUtilities;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace GearInfo
{
    public class Main : MelonMod
    {
        public bool isLoaded = false;

        public static AssetBundle UIBundle;

        public static string modsPath;

        public static string systemLanguage = "";

        public override void OnInitializeMelon()
        {
            modsPath = Path.GetFullPath(typeof(MelonMod).Assembly.Location + "/../../../Mods/");

            LocalizationManager.LoadJsonLocalization(LoadEmbeddedJSON("Localization.json"));
            UIBundle = LoadEmbeddedAssetBundle("giui");

            var handle = Addressables.LoadResourceLocationsAsync(GearItem.LoadGearItemPrefab("GEAR_Stick").GearItemData.PrefabReference);
            handle.WaitForCompletion();
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                int? c = 0;
                c = handle.Result[0]?.Dependencies?.Cast<Il2CppReferenceArray<IResourceLocation>>()?.Count;
                if (c != null)
                {
                    baseGameBundleNum = (int)c;
                }
                else
                {
                    Log(CC.Red, "Could not grab stick!");
                }
            }

            Settings.OnLoad();

            PreComputeArrowHitDamageMult();
        }

        //public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        //{
        //    if (IsMainMenu(sceneName) && string.IsNullOrEmpty(systemLanguage))
        //    {
        //        systemLanguage = Panel_OptionsMenu.GetLanguageStringFromSystemLang();
        //    }
        //}
    }
}




