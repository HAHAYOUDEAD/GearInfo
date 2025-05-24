using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections;
using Il2CppTLD.Gear;
using Il2CppTLD.SaveState;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;



//using Il2CppSystem.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine.AddressableAssets.ResourceLocators;

using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.UIElements;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;

namespace GearInfo
{
    internal class InfoHarvester
    {
        private class SurfaceResponse
        {
            public float hardness;
            public Vector2 velocityScalar;
            public float deflectScalar;
            public float retainedVelocity;
        }

        public static int baseGameBundleNum;

        private static readonly Dictionary<string, SurfaceResponse> surfaceHardness = new() 
        {
            { "Snow", new () { hardness = 0.05f * 50f * 1.5f, velocityScalar = new(0.1f,0.25f), deflectScalar = 0f } },
            { "Animal", new () { hardness = 0.35f * 50f * 1.5f, velocityScalar = new(0.15f,0.35f), deflectScalar = 0.2f } },
            { "Wood", new () { hardness = 0.5f * 50f * 1.5f, velocityScalar = new(0.3f,0.55f), deflectScalar = 0.3f } },
            { "Metal", new () { hardness = 0.75f * 50f * 1.5f, velocityScalar = new(0.6f,0.9f), deflectScalar = 0.8f } },
            { "Stone", new () { hardness = 0.98f * 50f * 1.5f, velocityScalar = new(0.35f,0.7f), deflectScalar = 0.85f } },
        };

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

        internal static void PreComputeArrowHitDamageMult()
        {
            foreach (var entry in surfaceHardness)
            {
                float deflectionThreshold = 75f * (1f - entry.Value.deflectScalar);
                float ratio = Mathf.Clamp01(-deflectionThreshold / (90f - deflectionThreshold));
                float retainedVelocity = Mathf.Lerp(entry.Value.velocityScalar.x, entry.Value.velocityScalar.y, ratio);
                
                entry.Value.retainedVelocity = retainedVelocity;
            }
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
             
            if (Localization.Language != "English") // doesn't work with mod items unless language first switched to english and the back to native
            {
                result[0] = Localization.Get("GI_EnglishName");
                result[1] = Localization.GetForFallbackLanguage(gear.GearItemData.DisplayNameLocID);
                return true;
            }
            //else if (Main.systemLanguage != "English") // not feasible, would need to preload the whole language table
            //{
            //    result[0] = Localization.Get("GI_LocalizedName");
            //    result[1] = Localization.GetForLang(gear.GearItemData.DisplayNameLocID, Main.systemLanguage);
            //    return true;
            //}

            return false;
        }

        internal static bool TryGetFoodDecayRates(GearItem gear, bool convertToDays, string[] result) 
        {
            Array.Clear(result, 0, result.Length);

            if (gear.GetComponent<FirstAidItem>())
            {
                return false;
            }

            if (gear.TryGetComponent(out FoodItem fi))
            {
                float decayIn = fi.m_DailyHPDecayInside / gear.GearItemData.MaxHP * 100f * Control.globalDifficultyMult;
                float decayOut = fi.m_DailyHPDecayOutside / gear.GearItemData.MaxHP * 100f * Control.globalDifficultyMult;
                float daysIn = gear.GearItemData.MaxHP / (fi.m_DailyHPDecayInside * Control.globalDifficultyMult);
                float daysOut = gear.GearItemData.MaxHP / (fi.m_DailyHPDecayOutside * Control.globalDifficultyMult);
                float daysLeftIn = gear.CurrentHP / (fi.m_DailyHPDecayInside * Control.globalDifficultyMult);
                float daysLeftOut = gear.CurrentHP / (fi.m_DailyHPDecayOutside * Control.globalDifficultyMult);

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

        internal static bool TryGetFoodPoisonChance(GearItem gear, bool calculateCurrent, string[] result)
        {
            Array.Clear(result, 0, result.Length);

            if (gear.GetComponent<FirstAidItem>())
            {
                return false;
            }

            bool adjustForDifficulty = Settings.options.adjustForDifficulty;

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

        internal static bool TryGetClothingDecayRates(GearItem gear, string[] result)
        {
            Array.Clear(result, 0, result.Length);

            if (gear.TryGetComponent(out ClothingItem ci))
            {
                float decayIn = ci.m_DailyHPDecayWhenWornInside / gear.GearItemData.MaxHP * 100f * Control.globalDifficultyMult;
                float decayOut = ci.m_DailyHPDecayWhenWornOutside / gear.GearItemData.MaxHP * 100f * Control.globalDifficultyMult;

                result[0] = Localization.Get("GI_ClothingWearOutRate");
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

        internal static bool TryGetDegradeOnUse(GearItem gear, string[] result, out bool isIcePick)
        {
            Array.Clear(result, 0, result.Length);

            isIcePick = false;

            if (gear.GetComponent<KeroseneLampItem>() || gear.GetComponent<FlashlightItem>() || gear.GetComponent<Bed>())
            {
                return false;
            }

            if (gear.TryGetComponent(out CookingPotItem cpi))
            {
                float degrade = cpi.m_ConditionPercentDamageFromBurningFood;

                result[0] = Localization.Get("GI_ToolDegrade");
                result[1] = $"{degrade}{Localization.Get("GI_PPMU")}";
                return true;
            }

            if (gear.TryGetComponent(out IceFishingHoleClearItem ice))
            { 
                isIcePick = true;

                result[2] = $"/ {Localization.Get("GI_ToolIceDamage")}";
                result[3] = $"{ice.m_HPDecreaseToClear / gear.GearItemData.MaxHP * 100f}{Localization.Get("GI_PPU")}";
            }

            if (gear.TryGetComponent(out DegradeOnUse dou))
            {
                float degrade = dou.m_DegradeHP;
                bool isWeapon = false;
                bool isAffectedBySkill = false; 

                if (gear.m_GunItem)
                {
                    isWeapon = true;

                    if (gear.m_GunItem && gear.m_GunItem.m_GunType == GunType.Rifle)
                    {
                        degrade *= GameManager.GetSkillRifle().GetConditionDegradeScale();
                        isAffectedBySkill = true;
                    }
                    if (gear.m_GunItem && gear.m_GunItem.m_GunType == GunType.Revolver)
                    {
                        degrade *= GameManager.GetSkillRevolver().GetConditionDegradeScale();
                        isAffectedBySkill = true;
                    }
                }

                if (gear.m_BowItem)
                {
                    degrade *= GameManager.GetSkillArchery().GetConditionDegradeScale();
                    isWeapon = true;
                    isAffectedBySkill = true;
                }

                result[0] = isWeapon ? Localization.Get("GI_WeaponDegrade") : Localization.Get("GI_ToolDegrade");
                result[1] = $"{(degrade / gear.GearItemData.MaxHP * 100f):0.#}{Localization.Get(isWeapon ? "GI_PPS" : "GI_PPU")}";
                if (isAffectedBySkill)
                {
                    result[1] += $" ({Localization.Get("GI_AffectedBySkill")})";
                }

                return true;
            }

            return false;
        }

        internal static bool TryGetDecayPerHour(GearItem gear, string[] result, out bool isCraftTool)
        {
            Array.Clear(result, 0, result.Length);

            isCraftTool = false;

            if (gear.GetComponent<FoodItem>() || gear.GetComponent<ClothingItem>())
            { 
                return false;
            }

            bool adjustForDifficulty = Settings.options.adjustForDifficulty;

            //add crafting decay of tools

            if (gear.TryGetComponent(out ToolsItem ti))
            {
                isCraftTool = true;

                result[2] = $"/ {Localization.Get("GI_Crafting")}";
                result[3] = $"{ti.m_DegradePerHourCrafting / gear.GearItemData.MaxHP * 100f}{Localization.Get("GI_PPH")}";

            }

            bool isBedRoll = gear.GetComponent<Bed>();

            if (gear.GearItemData.DailyHPDecay > 0)
            {
                float perDayDecay = gear.GearItemData.DailyHPDecay / gear.GearItemData.MaxHP * 100f * Control.globalDifficultyMult;
                float perWeekDecay = perDayDecay * 7f;
                float perMonthDecay = perDayDecay * 30f;

                float preferredDecay = perDayDecay;
                string decayText = Localization.Get("GI_PPD");

                if (perWeekDecay < 1f)
                {
                    preferredDecay = perMonthDecay;
                    decayText = Localization.Get("GI_PPM");
                }
                else if (perDayDecay < 1f)
                {
                    preferredDecay = perWeekDecay;
                    decayText = Localization.Get("GI_PPW");
                }

                result[0] = Localization.Get("Deterioration");
                result[1] = $"{preferredDecay:0.##}{decayText}";
                if (isBedRoll && gear.TryGetComponent(out DegradeOnUse bedDou))
                {
                    float bedDecay = bedDou.m_DegradeHP / gear.GearItemData.MaxHP * 100f * 24f;
                    result[1] += $" ({bedDecay:0.##}{Localization.Get("GI_PPDS")})";
                }
                return true;
            }

            return false;

        }
        internal static bool TryGetToolBreakChance(GearItem gear, string[] result)
        {
            Array.Clear(result, 0, result.Length);

            if (gear.TryGetComponent(out DegradeOnUse dou))
            {
                float threshold = gear.GearItemData.m_GearBreakConditionThreshold;

                float chance = gear.GetNormalizedCondition() / (threshold / 100f);
                chance = Mathf.Clamp(chance, 0f, 1f);
                chance = 1f - chance;
                chance = Mathf.Pow(chance, 2f);
                chance = Mathf.Lerp(0f, 100f, chance);

                result[0] = Localization.Get("GI_BreakThreshold");
                result[1] = $"{threshold}%";
                result[2] = $"/ {Localization.Get("GI_CurrentBreakChance")}";
                result[3] = $"{chance:0.##}%";
                return true;
            }

            return false;
        }


        internal static bool TryGetWeaponJamChance(GearItem gear, string[] result)
        {
            Array.Clear(result, 0, result.Length);

            if (gear.TryGetComponent(out GunItem gi))
            {
                vp_FPSWeapon weapon = GameManager.GetVpFPSCamera().GetWeaponFromItemData(gear.GetFPSItem());
                vp_FPSShooter? shooter = weapon?.GetComponent<vp_FPSShooter>();

                if (!shooter)
                {
                    return false;
                }

                float threshold = shooter.JamConditionThreshold;

                if (threshold <= 0f)
                {
                    return false;
                }

                float chance = 0f;

                float condition = gear.GetNormalizedCondition() * 100f;
                if (condition < threshold)
                {
                    chance = Mathf.Clamp01((threshold - condition) / threshold);
                    chance = Mathf.Lerp(shooter.JamMinimumChance, shooter.JamMaximumChance, chance);
                }
                result[0] = Localization.Get("GI_JamThreshold");
                result[1] = $"{threshold}%";
                result[2] = $"/ {Localization.Get("GI_CurrentJamChance")}";
                result[3] = $"{chance:0.##}%";
                return true;
            }

            return false;
        }

        internal static bool TryGetArrowInfo(GearItem gear, string[] result)
        {
            Array.Clear(result, 0, result.Length);

            if (gear.TryGetComponent(out ArrowItem ai))
            {
                result[0] = Localization.Get("GI_ArrowDurability");
                result[1] = $"{gear.GearItemData.MaxHP}";
                result[2] = $"/ {Localization.Get("GI_ArrowDamage")}";
                result[3] = $"{ai.m_VictimDamage}";
                return true;
            }

            return false;
        }
        internal static bool TryGetArrowHitConditionLoss(GearItem gear, string[] result)
        {
            Array.Clear(result, 0, result.Length);

            if (gear.TryGetComponent(out ArrowItem ai))
            {
                Dictionary<string, float> calculated = new();
                foreach (var entry in surfaceHardness)
                {
                    float hpLoss = (1 - ai.m_ImpactVelocityScalar * entry.Value.retainedVelocity) * entry.Value.hardness;
                    int final = Mathf.CeilToInt(Mathf.Clamp(hpLoss / gear.GearItemData.MaxHP * 100f, 1f, 100f));

                    calculated.Add(entry.Key, final);
                }

                result[0] = Localization.Get("GI_ArrowConditionLoss");
                result[1] = $"{Localization.Get("GI_HitSnow")} {calculated["Snow"]}%/ {Localization.Get("GI_HitAnimal")} {Mathf.CeilToInt(calculated["Animal"])}%/" +
                    $"{Localization.Get("GI_HitWood")} {Mathf.CeilToInt(calculated["Wood"])}%/ {Localization.Get("GI_HitStone")} {Mathf.CeilToInt(calculated["Stone"])}%";
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
