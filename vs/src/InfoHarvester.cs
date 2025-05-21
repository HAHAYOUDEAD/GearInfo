using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections;
using Il2CppTLD.SaveState;

//using Il2CppSystem.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine.AddressableAssets.ResourceLocators;

using UnityEngine.ResourceManagement.ResourceLocations;

namespace GearInfo
{
    internal class InfoHarvester
    {
        public static int baseGameBundleNum;



        public enum ButtonType
        {
            None,
            Copy,
            Switch
        }

        public enum AltInfoType
        { 
            None,
            Decay,
            Poisoning,
        }

        internal static bool TryGetItemOrigin(GearItem gear, string[] result)
        {
            Array.Clear(result, 0, result.Length);
            
            // check if mod
            var modComponentAssembly = typeof(ModComponent.API.Components.ModBaseComponent).Assembly;
            var processorType = modComponentAssembly.GetType("ModComponent.Utils.AssetBundleProcessor");

            Traverse processorTraverse = Traverse.Create(processorType);
            string? output = processorTraverse.Method("GetPrefabBundlePath", gear.name).GetValue() as string;

            if (!string.IsNullOrEmpty(output))
            {
                string? folder = processorTraverse.Property("tempFolderName").GetValue() as string;
                if (!string.IsNullOrEmpty(folder))
                {
                    string[] split = output.Split($"{folder}\\"); // split full bundle path at MC folder name
                    split = split[1].Split("\\"); // split at \
                    string final = char.ToUpper(split[0][0]) + split[0].Substring(1); // capitalize first letter

                    result[0] = Localization.Get("GI_ModName");
                    result[1] = Regex.Replace(final, @"(?<=[a-z])(?=[A-Z])", " "); // insert spaces before each capital letter
                    return true;
                }
            }

            // check if dlc
            var handle = Addressables.LoadResourceLocationsAsync(gear.GearItemData.PrefabReference);
            handle.WaitForCompletion();
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                foreach (IResourceLocation loc in handle.Result.Cast<Il2CppReferenceArray<IResourceLocation>>())
                {
                    var depArray = loc.Dependencies.Cast<Il2CppReferenceArray<IResourceLocation>>();
                    if (depArray.Count <= baseGameBundleNum)
                    {
                        return false;
                    }
                    foreach (IResourceLocation dep in depArray)
                    {
                        if (dep.InternalId.StartsWith("DLC01"))
                        {
                            result[0] = Localization.Get("GI_DLCName");
                            result[1] = Localization.Get("DLC_DLC01");
                            handle.Release();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        internal static bool TryGetItemWeight(GearItem gear, string[] result)
        {
            Array.Clear(result, 0, result.Length);
             
            bool freedom = InterfaceManager.GetPanel<Panel_OptionsMenu>().State.m_Units == MeasurementUnits.Imperial;
            string weight = $"{gear.GetItemWeightKG().ToStringMetric()} {Localization.Get("GAMEPLAY_kgUnits")}";
            string weightFreedom = $"{gear.GetItemWeightKG().ToStringImperial()} {Localization.Get("GAMEPLAY_lbsUnits")}";

            result[0] = Localization.Get("GI_Weight");
            result[1] = $"{(freedom ? weightFreedom : weight)} ({(freedom ? weight : weightFreedom)})";
            return true;
        }

        internal static bool TryGetLocalizedName(GearItem gear, string[] result)
        {
            Array.Clear(result, 0, result.Length);
             
            if (Localization.Language != "English")
            {
                result[0] = Localization.Get("GI_EnglishName");
                result[1] = Localization.GetForLang(gear.GearItemData.DisplayNameLocID, "English");
                return true;
            }
            else if (Main.systemLanguage != "English") // doesn't work?
            {
                result[0] = Localization.Get("GI_LocalizedName");
                result[1] = Localization.GetForLang(gear.GearItemData.DisplayNameLocID, Main.systemLanguage);
                return true;
            }

            return false;
        }

        internal static bool TryGetFoodDecayRates(GearItem gear, bool convertToDays, bool adjustForDifficulty, string[] result) 
        {
            Array.Clear(result, 0, result.Length);

            if (gear.TryGetComponent(out FoodItem fi))
            {
                float mult = adjustForDifficulty ? GameManager.GetExperienceModeManagerComponent().GetDecayScale() : 1f;

                float decayIn = fi.m_DailyHPDecayInside / gear.GearItemData.MaxHP * 100f * mult;
                float decayOut = fi.m_DailyHPDecayOutside / gear.GearItemData.MaxHP * 100f * mult;
                float daysIn = gear.GearItemData.MaxHP / (fi.m_DailyHPDecayInside * mult);
                float daysOut = gear.GearItemData.MaxHP / (fi.m_DailyHPDecayOutside * mult);
                float daysLeftIn = gear.CurrentHP / (fi.m_DailyHPDecayInside * mult);
                float daysLeftOut = gear.CurrentHP / (fi.m_DailyHPDecayOutside * mult);

                result[0] = Localization.Get(convertToDays ? "GI_ShelfLife" : "GI_DecayRate");
                result[1] = convertToDays ?
                    $"{daysLeftIn.ToString(FastApproximately(daysLeftIn, daysIn, 0.05f) ? "F0" : "F1")}/ {daysIn:F0} {Localization.Get("GI_Days")}" :
                    $"{decayIn:F2}{Localization.Get("GI_PPD")}";
                result[2] = $"/ {Localization.Get("GI_Outdoors")}";
                result[3] = convertToDays ? 
                    $"{daysLeftOut.ToString(FastApproximately(daysLeftOut, daysOut, 0.05f) ? "F0" : "F1")}/ {daysOut:F0} {Localization.Get("GI_Days")}" : 
                    $"{decayOut:F2}{Localization.Get("GI_PPD")}";
                return true;
            }

            return false;
        }

        internal static bool TryGetFoodPoisonChance(GearItem gear, bool calculateCurrent, bool adjustForDifficulty, string[] result)
        {
            Array.Clear(result, 0, result.Length);

            if (gear.TryGetComponent(out FoodItem fi))
            {
                bool isMeat = fi.m_IsRawMeat;
                float chance = fi.m_ChanceFoodPoisoning;
                float chance20 = fi.m_ChanceFoodPoisoningLowCondition;
                float chance0 = fi.m_ChanceFoodPoisoningRuined;
                float condition = gear.GetNormalizedCondition();

                float currentChance = 0f;

                float caloriesRemaining = fi.m_CaloriesRemaining;

                string describedCalculated = "";

                if (calculateCurrent)
                {
                    bool disabled = adjustForDifficulty && GameManager.InCustomMode() && !GameManager.GetCustomMode().m_EnableFoodPoisoning;

                    if (!disabled)
                    {
                        bool notMeat75 = !isMeat && condition > 0.745f;
                        bool morsel = caloriesRemaining < 5f;
                        bool wornOut = gear.IsWornOut();
                        bool below20 = condition < 0.2f;

                        List<string> factors = new();

                        if (!morsel && isMeat && condition > 0.745f) factors.Add(Localization.Get("GI_RawMeat"));

                        if (morsel || notMeat75)
                        {
                            if (morsel)
                            {
                                factors.Add(Localization.Get("GI_InsignificantCalories"));
                            }
                            else if (notMeat75)
                            {
                                factors.Add(Localization.Get("GI_Above75Condition"));
                            }
                            currentChance = 0f;
                        }
                        else if (wornOut)
                        {
                            factors.Add(Localization.Get("GI_WornOut"));
                            currentChance = chance0;
                        }
                        else if (below20)
                        {
                            factors.Add(Localization.Get("GI_Below20Condition"));
                            currentChance = chance20;
                        }
                        else
                        {
                            if (factors.Count == 0)
                            {
                                factors.Add(Localization.Get("GI_BasePoisonChance"));
                            }
                            currentChance = chance;
                        }

                        describedCalculated = string.Join("/ ", factors.ToArray());
                        describedCalculated = $"{currentChance}% ({describedCalculated})";
                    }
                    else
                    {
                        describedCalculated = $"0% ({Localization.Get("GI_FromDifficulty")})";
                    }
                }

                result[0] = Localization.Get(calculateCurrent ? "GI_CurrentPoisonChance" : "GI_PoisonChance");
                result[1] = calculateCurrent ? 
                   describedCalculated :
                   $"{chance}% ({chance20}% {Localization.Get("GI_AtLowCondition")} | {chance0}% {Localization.Get("GI_WhenRuined")})";
                return true;
            }

            return false;
        }

        internal static bool TryGetThirstAndVitC(GearItem gear, string[] result)
        {
            Array.Clear(result, 0, result.Length);

            if (gear.TryGetComponent(out FoodItem fi))
            {
                float thirst = fi.m_ReduceThirst;
                float vitc = 0f;

                foreach (var n in fi.m_Nutrients)
                {
                    if (n.NutrientDefinition.name.Contains("VitaminC"))
                    {
                        vitc = n.m_Amount * (fi.GetComponent<FoodWeight>() ? gear.GetItemWeightKG().ToQuantity(1f) : 
                            gear.GetItemWeightKG().ToQuantity(1f) / gear.GearItemData.BaseWeightKG.ToQuantity(1f));
                        break;
                    }
                }

                result[0] = thirst < 0 ? Localization.Get("GI_ThirstAdd") : Localization.Get("GI_ThirstQuench");
                result[1] = $"{Mathf.Abs(thirst):F0}";
                result[2] = $"/ {Localization.Get("GI_VitaminC")}";
                result[3] = vitc.ToString("F0");
                return true;
            }

            return false;
        }

        internal static bool TryGetClothingDecayRates(GearItem gear, bool adjustForDifficulty, string[] result)
        {
            Array.Clear(result, 0, result.Length);

            if (gear.TryGetComponent(out ClothingItem ci))
            {
                float mult = adjustForDifficulty ? GameManager.GetExperienceModeManagerComponent().GetDecayScale() : 1f;

                float decayIn = ci.m_DailyHPDecayWhenWornInside / gear.GearItemData.MaxHP * 100f * mult;
                float decayOut = ci.m_DailyHPDecayWhenWornOutside / gear.GearItemData.MaxHP * 100f * mult;

                result[0] = Localization.Get("GI_WearOutRate");
                result[1] = $"{decayIn:F2}{Localization.Get("GI_PPD")}";
                result[2] = $"/ {Localization.Get("GI_Outdoors")}";
                result[3] = $"{decayOut:F2}{Localization.Get("GI_PPD")}";
                return true;
            }

            return false;
        }

        internal static bool TryGetClothingBonuses(GearItem gear, string[] result)
        {
            Array.Clear(result, 0, result.Length);

            if (gear.TryGetComponent(out ClothingItem ci))
            {
                float warmth = ci.m_Warmth;
                float windp = ci.m_Windproof;
                bool freedom = InterfaceManager.GetPanel<Panel_OptionsMenu>().State.m_Units == MeasurementUnits.Imperial;

                string warmthSign = warmth > 0 ? "+" : "";
                string windpSign = windp > 0 ? "+" : "";
                string warmthText = Utils.GetTemperatureString(warmth, true, false, warmth > 0);
                string warmthTextAlt = warmthSign + (freedom ? $"{warmth:F1}{Localization.Get("GAMEPLAY_DegreesCUnits")}" : 
                                                               $"{(warmth * 1.8f):F1}{Localization.Get("GAMEPLAY_DegreesFUnits")}");
                string windpText = Utils.GetTemperatureString(windp, true, false, windp > 0);
                string windpTextAlt = windpSign + (freedom ? $"{windp:F1}{Localization.Get("GAMEPLAY_DegreesCUnits")}" :
                                                               $"{(windp * 1.8f):F1}{Localization.Get("GAMEPLAY_DegreesFUnits")}");

                result[0] = Localization.Get("GI_Warmth");
                result[1] = $"{warmthText} ({warmthTextAlt})";
                result[2] = $"/ {Localization.Get("GI_Windproofness")}";
                result[3] = $"{windpText} ({windpTextAlt})";
                return true;
            }

            return false;
        }

        internal static bool TryGetIsCat(GearItem gear, string[] result)
        {
            Array.Clear(result, 0, result.Length);

            if (gear.name.ToLower().Contains("cat"))
            {
                result[0] = Localization.Get("GI_IsCat");
                result[1] = Localization.Get("GI_CouldBeACat");
                return true;
            }

            result[0] = Localization.Get("GI_IsCat");
            result[1] = Localization.Get("GI_AbsolutelyNotACat");
            return true;
        }

    }
}
