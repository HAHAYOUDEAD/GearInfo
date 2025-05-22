using Il2CppEasyRoads3Dv3;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using Il2CppVLB;
using UnityEngine;

namespace GearInfo
{
    internal class Control
    {
        public static GameObject GIUIRoot;
        public static RectTransform Anchor;
        public static Button MainButton;
        public static GameObject MainWindow;
        public static Toggle DifficultyToggle;
        public static GameObject SingleEntry;
        public static GameObject DoubleEntry;
        public static Transform WindowListRoot;

        private static Queue<GameObject> singleEntryPool = new();
        private static Queue<GameObject> doubleEntryPool = new();
        private static List<GameObject> activeEntries = new();

        private static bool buttonOnLeftSide = false;
        private static float buttonOffset = 25f;

        public static Color separatorColor = new Color (0.98f, 0.98f, 0.98f, 0.2f);

        private static string[] globalTextArray = new string[4];
        private static string[] globalAltTextArray = new string[4];

        public static void SetupUI()
        {
            if (GIUIRoot == null)
            {
                GIUIRoot = GameObject.Instantiate(Main.UIBundle.LoadAsset<GameObject>("GIUICanvas"));
                GameObject.DontDestroyOnLoad(GIUIRoot);
                MainButton = GIUIRoot.transform.Find("MainButton").GetComponent<Button>();
                Anchor = GIUIRoot.transform.Find("MainButtonAnchor").GetComponent<RectTransform>();
                MainWindow = GIUIRoot.transform.Find("Window").gameObject;
                DifficultyToggle = MainWindow.transform.Find("DifficultyToggle").GetComponent<Toggle>();
                WindowListRoot = MainWindow.transform.Find("VLayout");
                SingleEntry = GameObject.Instantiate(Main.UIBundle.LoadAsset<GameObject>("SingleEntry"));
                GameObject.DontDestroyOnLoad(SingleEntry);
                DoubleEntry = GameObject.Instantiate(Main.UIBundle.LoadAsset<GameObject>("DoubleEntry"));
                GameObject.DontDestroyOnLoad(DoubleEntry);
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
            Transform rs = pi.transform.Find("Right Side");
            MelonCoroutines.Start(NudgeVanillaUI(rs.Find("GearItem"), 30f, reset, instant));
            rs.Find("GearStatsBlock(Clone)/ItemDescriptionLabel").gameObject.active = reset;
        }

        public static IEnumerator NudgeVanillaUI(Transform element, float amount, bool reset, bool instant = false)
        {
            if (reset)
            {
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
                    yield return new WaitForEndOfFrame();
                }
            }
            else
            {
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
                var localResult = globalTextArray;
                var localAltResult = globalAltTextArray;

                comp.SetButton(ButtonType.Switch, () => SwitchButtonAction(localComp, localResult, localAltResult));
            }

                // Poisoning
            if (TryGetFoodPoisonChance(gi, true, globalTextArray))
            {
                entry = PrepareNewEntry(SingleEntry, "InfoFoodPoisoning", out comp);

                TryGetFoodPoisonChance(gi, false, globalAltTextArray);
                comp.SetFields(Settings.options.foodPoisoningAlt ? globalAltTextArray : globalTextArray, AltInfoType.Poisoning);

                var localComp = comp;
                var localResult = globalTextArray;
                var localAltResult = globalAltTextArray;

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
            if (TryGetDegradeOnUse(gi, globalTextArray, out bool isIcePick))
            {
                entry = PrepareNewEntry(isIcePick ? DoubleEntry : SingleEntry, "InfoToolDegrade", out comp);
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


            if (TryGetIsCat(gi, globalTextArray) && Il2Cpp.Utils.RollChance(0.5f))
            {
                entry = PrepareNewEntry(SingleEntry, "InfoIsCat", out comp);
                comp.SetFields(globalTextArray);
            }

            // Rearrange entries
            for (int i = 0; i < activeEntries.Count; i++)
            {
                activeEntries[i].transform.SetSiblingIndex(i);
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
            comp = go.AddComponent<GearInfoUIEntry>();
            activeEntries.Add(go);
            return go;
        }

        private static void ClearEntries()
        {
            foreach (var entry in activeEntries)
            {
                var comp = entry.GetComponent<GearInfoUIEntry>();
                if (!comp)
                {
                    MelonLogger.Msg(CC.Red, $"Entry {entry.name} has no component");
                    continue;
                }
                comp.ClearListeners();
                entry.SetActive(false); // don't destroy!
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
            Vector2 pos = Anchor.anchoredPosition;
            UILabel label = InterfaceManager.GetPanel<Panel_Inventory>().m_ItemDescriptionPage.m_ItemNameLabel;
            float conversion = label.root.m_AspectRatioForScaling / 2f; // ratio the label size and divide by 2 because button origin is in the middle of label
            float distance = buttonOffset;
            if (buttonOnLeftSide) pos.x -= label.mCalculatedSize.x * conversion + distance;
            else pos.x += label.mCalculatedSize.x * conversion + distance;

            MainButton.GetComponent<RectTransform>().anchoredPosition = pos;
        }
    }
}
