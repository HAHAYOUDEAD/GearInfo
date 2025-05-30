﻿using Il2CppEasyRoads3Dv3;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using Il2CppVLB;
using UnityEngine;

namespace GearInfo
{
    internal class Control
    {
        public static GameObject GIUIRoot;
        private static RectTransform MainButtonAnchor;
        private static Button MainButton;
        private static GameObject MainWindow;
        private static Toggle DifficultyToggle;
        private static GameObject SingleEntry;
        private static GameObject DoubleEntry;
        private static GameObject EmptyEntry;
        private static Transform WindowListRoot;

        private static Queue<GameObject> singleEntryPool = new();
        private static Queue<GameObject> doubleEntryPool = new();
        private static Queue<GameObject> emptyEntryPool = new();
        private static List<GameObject> activeEntries = new();

        //private static UITexture gearInventoryIcon;
        //private static UITexture clothingInventoryIcon;
        //private static Color gearInventoryColor = new Color(1f, 1f, 1f, 0.45f);

        private const int maxEntries = 9;

        private static bool buttonOnLeftSide = false;
        private static float buttonOffset = 25f;

        //public static Color separatorColor = new Color (0.98f, 0.98f, 0.98f, 0.2f);

        private static string[] globalTextArray = new string[4];
        private static string[] globalAltTextArray = new string[4];

        public static float globalDecayMult = 1f;

        public static void SetupUI()
        {
            if (GIUIRoot == null)
            {
                GIUIRoot = GameObject.Instantiate(Main.UIBundle.LoadAsset<GameObject>("GIUICanvas"));
                GameObject.DontDestroyOnLoad(GIUIRoot);
                GIUIRoot.name = "GIUICanvas";
                GIUIRoot.active = false;
                MainButton = GIUIRoot.transform.Find("MainButton").GetComponent<Button>();
                MainButtonAnchor = GIUIRoot.transform.Find("MainButtonAnchor").GetComponent<RectTransform>();
                MainWindow = GIUIRoot.transform.Find("Window").gameObject;
                DifficultyToggle = MainWindow.transform.Find("DifficultyToggle").GetComponent<Toggle>();
                WindowListRoot = MainWindow.transform.Find("VLayout");
                SingleEntry = GameObject.Instantiate(Main.UIBundle.LoadAsset<GameObject>("SingleEntry"));
                GameObject.DontDestroyOnLoad(SingleEntry);
                SingleEntry.name = "GIUIEntrySingle";
                SingleEntry.active = false;
                DoubleEntry = GameObject.Instantiate(Main.UIBundle.LoadAsset<GameObject>("DoubleEntry"));
                GameObject.DontDestroyOnLoad(DoubleEntry);
                DoubleEntry.name = "GIUIEntryDouble";
                DoubleEntry.active = false;
                EmptyEntry = GameObject.Instantiate(Main.UIBundle.LoadAsset<GameObject>("EmptyEntry"));
                GameObject.DontDestroyOnLoad(EmptyEntry);
                EmptyEntry.name = "GIUIEntryEmpty";
                EmptyEntry.active = false;
                MainButton.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(MainButtonAction)));
                DifficultyToggle.isOn = Settings.options.adjustForDifficulty;
                DifficultySwitchReplaceGraphic(DifficultyToggle.isOn);
                DifficultyToggle.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<bool>>(DifficultySwitchAction));
                MainWindow.active = false;
            }
        }

        public static void AdjustUIPosition(bool reset, bool instant = false)
        {
            Panel_Inventory pi = InterfaceManager.GetPanel<Panel_Inventory>();
            //if (gearInventoryIcon == null) gearInventoryIcon = pi.m_GearItemCoverflow.m_Texture;
            //if (clothingInventoryIcon == null) clothingInventoryIcon = pi.m_GearItemCoverflow.m_TextureWithDamage;
            Transform rs = pi.transform.Find("Right Side");
            MelonCoroutines.Start(NudgeVanillaUI(rs.Find("GearItem"), 60f, reset, instant));
            rs.Find("GearStatsBlock(Clone)/ItemDescriptionLabel").gameObject.active = reset;
        }

        public static IEnumerator NudgeVanillaUI(Transform element, float amount, bool reset, bool instant = false)
        {
            if (reset)
            {
                //gearInventoryIcon.color = Color.white;
                //clothingInventoryIcon.color = Color.white;
                element.localPosition = Vector3.zero;
                yield break;
            }

            Vector3 endpos = Vector3.up * amount;

            if (!instant)
            {
                float t = 0f;
                Vector3 startpos = Vector3.zero;

                while (t <= 1f)
                {
                    t += Time.deltaTime / 0.1f;
                    element.localPosition = Vector3.Lerp(startpos, endpos, Mathf.Pow(t - 1, 3f) + 1);
                    //gearInventoryIcon.color = Color.Lerp(Color.white, gearInventoryColor, t);
                    //clothingInventoryIcon.color = Color.Lerp(Color.white, gearInventoryColor, t);
                    yield return new WaitForEndOfFrame();
                }
            }
            else
            {
                //gearInventoryIcon.color = gearInventoryColor;
                //clothingInventoryIcon.color = gearInventoryColor;
                element.localPosition = endpos;
            }
            yield break;
        }

        public static void SetupRelevantData(GearItem? gi = null)
        {
            if (gi == null)
            {
                gi = InterfaceManager.GetPanel<Panel_Inventory>().GetCurrentlySelectedItem()?.m_GearItem;
            }
            if (gi == null) return;

            

            GameObject? entry = null;
            GearInfoUIEntry? comp = null;

            ClearEntries(); // pool entries

            // General section
                // Mod/DLC
            if (TryGetItemOrigin(gi, globalTextArray))
            {
                entry = PrepareNewEntry(SingleEntry, "InfoOrigin", out comp);
                comp.SetFields(globalTextArray);
            }

                // Prefab name
            entry = PrepareNewEntry(SingleEntry, "InfoName", out comp);
            comp.SetFields([Localization.Get("GI_PrefabName"), gi.name]);
            comp.SetButton(ButtonType.Copy, () => CopyButtonAction(gi.name));

                // Localized name
            if (TryGetLocalizedName(gi, globalTextArray))
            {
                entry = PrepareNewEntry(SingleEntry, "InfoLocalizedName", out comp);
                comp.SetFields(globalTextArray);
            }

                // Weight in units
            if (TryGetItemWeight(gi, globalTextArray))
            {
                entry = PrepareNewEntry(SingleEntry, "InfoWeight", out comp);
                comp.SetFields(globalTextArray);
            }

            // Food section
                // Decay
            if (TryGetFoodDecayRates(gi, true, globalTextArray))
            {
                entry = PrepareNewEntry(DoubleEntry, "InfoFoodDecay", out comp);

                TryGetFoodDecayRates(gi, false, globalAltTextArray);
                comp.SetFields(Settings.options.decayAlt ? globalAltTextArray : globalTextArray, AltInfoType.Decay);

                var localComp = comp; // otherwise global vars are captured in button action lambda
                var localResult = globalTextArray.ToArray();
                var localAltResult = globalAltTextArray.ToArray();

                comp.SetButton(ButtonType.Switch, () => SwitchButtonAction(localComp, localResult, localAltResult));
            }

                // Poisoning
            if (TryGetFoodPoisonChance(gi, true, globalTextArray))
            {
                entry = PrepareNewEntry(SingleEntry, "InfoFoodPoisoning", out comp);

                TryGetFoodPoisonChance(gi, false, globalAltTextArray);
                comp.SetFields(Settings.options.foodPoisoningAlt ? globalAltTextArray : globalTextArray, AltInfoType.Poisoning);

                var localComp = comp;
                var localResult = globalTextArray.ToArray();
                var localAltResult = globalAltTextArray.ToArray();

                comp.SetButton(ButtonType.Switch, () => SwitchButtonAction(localComp, localResult, localAltResult));
            }

                // Thirst and vitamin C
            if (TryGetThirstAndVitC(gi, globalTextArray))
            {
                entry = PrepareNewEntry(DoubleEntry, "InfoThirstVitC", out comp);
                comp.SetFields(globalTextArray);
            }

            // Clothing section
                // Decay
            if (TryGetClothingDecayRates(gi, globalTextArray))
            {
                entry = PrepareNewEntry(DoubleEntry, "InfoClothingDecay", out comp);
                comp.SetFields(globalTextArray);
            }
                //Clothing bonuses
            if (TryGetClothingBonuses(gi, globalTextArray))
            {
                entry = PrepareNewEntry(DoubleEntry, "InfoClothingBonuses", out comp);
                comp.SetFields(globalTextArray);
            }

            // Tool section
                // Degrade on use
            if (TryGetDegradeOnUse(gi, globalTextArray, false, out bool isIcePick, out bool isCraftTool))
            {
                entry = PrepareNewEntry(isIcePick || isCraftTool ? DoubleEntry : SingleEntry, "InfoToolDegrade", out comp);


                if (isIcePick && isCraftTool) // add switch if too many values
                {
                    TryGetDegradeOnUse(gi, globalAltTextArray, true, out bool _, out bool _);
                    comp.SetFields(Settings.options.toolUseAlt ? globalAltTextArray : globalTextArray, AltInfoType.ToolUse);

                    var localComp = comp;
                    var localResult = globalTextArray.ToArray();
                    var localAltResult = globalAltTextArray.ToArray();

                    comp.SetButton(ButtonType.Switch, () => SwitchButtonAction(localComp, localResult, localAltResult));
                }
                else
                {
                    comp.SetFields(globalTextArray);
                }

                // switch
            }
            // Degrade per time
            if (TryGetDecayPerHour(gi, globalTextArray))
            {
                entry = PrepareNewEntry(SingleEntry, "InfoTimedDecay", out comp);
                comp.SetFields(globalTextArray);
            }



                // Break chance
            if (TryGetToolBreakChance(gi, globalTextArray))
            {
                entry = PrepareNewEntry(DoubleEntry, "InfoTookBreakChance", out comp);
                comp.SetFields(globalTextArray);
            }
                // Arrow stuff
            if (TryGetArrowInfo(gi, globalTextArray))
            {
                entry = PrepareNewEntry(DoubleEntry, "InfoArrows", out comp);
                comp.SetFields(globalTextArray);
            }
                // Arrow condition loss
            if (TryGetArrowHitConditionLoss(gi, globalTextArray))
            {
                entry = PrepareNewEntry(SingleEntry, "InfoArrowHit", out comp);
                comp.SetFields(globalTextArray);
            }
                // Weapon jam chance
            if (TryGetWeaponJamChance(gi, globalTextArray))
            {
                entry = PrepareNewEntry(DoubleEntry, "InfoWeaponJamChance", out comp);
                comp.SetFields(globalTextArray);
            }
                // Bow damage
            if (TryGetBowDamageMult(gi, globalTextArray))
            {
                entry = PrepareNewEntry(SingleEntry, "InfoBowDamage", out comp);
                comp.SetFields(globalTextArray);
            }

            // Firestarting
                // Time to start fire
            if (TryGetFireStarterTime(gi, false, globalTextArray))
            {
                entry = PrepareNewEntry(DoubleEntry, "InfoTimeStartFire", out comp);

                TryGetFireStarterTime(gi, true, globalAltTextArray);
                comp.SetFields(Settings.options.fireStartAlt ? globalAltTextArray : globalTextArray, AltInfoType.Firestarting);

                var localComp = comp;
                var localResult = globalTextArray.ToArray();
                var localAltResult = globalAltTextArray.ToArray();

                comp.SetButton(ButtonType.Switch, () => SwitchButtonAction(localComp, localResult, localAltResult));
            } 
                // Time to start fire
            if (TryGetFuelBurnInfo(gi, false, globalTextArray))
            {
                entry = PrepareNewEntry(DoubleEntry, "InfoFuelBurn", out comp);

                TryGetFuelBurnInfo(gi, true, globalAltTextArray);
                comp.SetFields(Settings.options.fuelInfoAlt ? globalAltTextArray : globalTextArray, AltInfoType.Fuel);

                var localComp = comp;
                var localResult = globalTextArray.ToArray();
                var localAltResult = globalAltTextArray.ToArray();

                comp.SetButton(ButtonType.Switch, () => SwitchButtonAction(localComp, localResult, localAltResult));
            }
                // Light time
            if (TryGetLightSourceBurningTime(gi, globalTextArray))
            {
                entry = PrepareNewEntry(DoubleEntry, "InfoLightTime", out comp);
                comp.SetFields(globalTextArray);
            }


            // meme
            if (TryGetIsCat(gi, globalTextArray) && Il2Cpp.Utils.RollChance(0.2f))
            {
                entry = PrepareNewEntry(SingleEntry, "InfoIsCat", out comp);
                comp.SetFields(globalTextArray);
            }

            // Rearrange entries
            for (int i = 0; i < activeEntries.Count; i++)
            {
                activeEntries[i].transform.SetSiblingIndex(i);
            }

            if (activeEntries.Count < maxEntries)
            {
                FillWithEmpty(maxEntries - activeEntries.Count);
            }
        }

        public static void PrepareWindow()
        {
            TMP_Text difTogField = DifficultyToggle.GetComponentInChildren<TMP_Text>();
            difTogField.text = Localization.Get("GI_AdjustForDifficulty");
            difTogField.font = AquireFont();
        }

        private static GameObject PrepareNewEntry(GameObject entryPrefab, string name, out GearInfoUIEntry comp)
        {
            GameObject go;
            Queue<GameObject> pool = entryPrefab == SingleEntry ? singleEntryPool : doubleEntryPool;

            if (pool.Count > 0)
            {
                go = pool.Dequeue();
                go.name = name;
                go.SetActive(true);
                comp = go.GetOrAddComponent<GearInfoUIEntry>();
                activeEntries.Add(go);
                return go;
            }
            go = GameObject.Instantiate(entryPrefab, WindowListRoot);
            go.name = name;
            go.SetActive(true);
            comp = go.AddComponent<GearInfoUIEntry>();
            activeEntries.Add(go);
            return go;
        }

        public static void FillWithEmpty(int num)
        {
            GameObject go;

            for (int i = 0; i < num; i++)
            {
                if (emptyEntryPool.Count >= num)
                {
                    go = emptyEntryPool.Dequeue();
                }
                else
                {
                    go = GameObject.Instantiate(EmptyEntry, WindowListRoot);
                }
                go.name = "Filler";
                go.SetActive(true);
                activeEntries.Add(go);
            }
        }

        private static void ClearEntries()
        {
            foreach (var entry in activeEntries)
            {
                var comp = entry.GetComponent<GearInfoUIEntry>();
                if (!comp)
                {
                    entry.SetActive(false);
                    emptyEntryPool.Enqueue(entry);
                    continue;
                }
                comp.ClearListeners();
                entry.SetActive(false); 
                if (comp.fields.Length > 2) doubleEntryPool.Enqueue(entry);
                else singleEntryPool.Enqueue(entry);
            }
            activeEntries.Clear();
        }

        private static void CopyButtonAction(string text)
        {
            GUIUtility.systemCopyBuffer = text;
            HUDMessage.AddMessage(Localization.Get("GI_Message_Copied"), true, true);
        }
        private static void SwitchButtonAction(GearInfoUIEntry comp, string[] fields, string[] altFields)
        {
            switch (comp.altInfoType)
            { 
                case AltInfoType.Poisoning:
                    if (Settings.options.foodPoisoningAlt) comp.SetFields(fields);
                    else comp.SetFields(altFields);
                    Settings.options.foodPoisoningAlt = !Settings.options.foodPoisoningAlt;
                    break;
                case AltInfoType.Decay:
                    if (Settings.options.decayAlt) comp.SetFields(fields);
                    else comp.SetFields(altFields);
                    Settings.options.decayAlt = !Settings.options.decayAlt;
                    break;
                case AltInfoType.ToolUse:
                    if (Settings.options.toolUseAlt) comp.SetFields(fields);
                    else comp.SetFields(altFields);
                    Settings.options.toolUseAlt = !Settings.options.toolUseAlt;
                    break;
                case AltInfoType.Firestarting:
                    if (Settings.options.fireStartAlt) comp.SetFields(fields);
                    else comp.SetFields(altFields);
                    Settings.options.fireStartAlt = !Settings.options.fireStartAlt;
                    break;
                case AltInfoType.Fuel:
                    if (Settings.options.fuelInfoAlt) comp.SetFields(fields);
                    else comp.SetFields(altFields);
                    Settings.options.fuelInfoAlt = !Settings.options.fuelInfoAlt;
                    break;

            }

            Settings.options.Save();
        }

        private static void MainButtonAction() => ToggleWindow(!IsWindowEnabled());

        public static void ToggleWindow(bool enable)
        {
            if (GIUIRoot)
            {
                AdjustUIPosition(!enable);

                MainWindow.active = enable;

                if (enable)
                {
                    PrepareWindow();
                    SetupRelevantData();
                }
            }
        }

        public static bool IsWindowEnabled()
        {
            if (GIUIRoot)
            {
                if (MainWindow)
                {
                    return MainWindow.active;
                }
                else
                {
                    return false;
                }
            }

            Log(CC.Red, "GIUI not initialized. WindowEnabled");
            return false;
        }

        private static void DifficultySwitchAction(bool isOn)
        {
            DifficultySwitchReplaceGraphic(isOn);

            Settings.options.adjustForDifficulty = isOn;
            Settings.options.Save();

            globalDecayMult = isOn ? GameManager.GetExperienceModeManagerComponent().GetDecayScale() : 1f;

            SetupRelevantData();
        }

        private static void DifficultySwitchReplaceGraphic(bool isOn)
        {
            Image on = DifficultyToggle.transform.Find("Off").GetComponent<Image>();
            Image off = DifficultyToggle.transform.Find("Disabled").GetComponent<Image>();
            Image highlight = DifficultyToggle.transform.Find("On").GetComponent<Image>();
            Image click = DifficultyToggle.transform.Find("Click").GetComponent<Image>();

            on.gameObject.SetActive(isOn);
            off.gameObject.SetActive(!isOn);

            DifficultyToggle.targetGraphic = isOn ? on : off;
            SpriteState ss = new SpriteState()
            {
                highlightedSprite = isOn ? highlight.sprite : on.sprite,
                pressedSprite = isOn ? click.sprite : highlight.sprite
            };
            DifficultyToggle.spriteState = ss;
        }

        public static void CalculateMainButtonPosition()
        {
            Vector2 pos = MainButtonAnchor.anchoredPosition;
            UILabel label = InterfaceManager.GetPanel<Panel_Inventory>().m_ItemDescriptionPage.m_ItemNameLabel;
            float conversion = label.root.m_AspectRatioForScaling / 2f; // ratio the label size and divide by 2 because button origin is in the middle of label
            float distance = buttonOffset;
            if (buttonOnLeftSide) pos.x -= label.mCalculatedSize.x * conversion + distance;
            else pos.x += label.mCalculatedSize.x * conversion + distance;

            MainButton.GetComponent<RectTransform>().anchoredPosition = pos;
        }
    }
}
