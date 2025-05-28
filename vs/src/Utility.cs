global using CC = System.ConsoleColor;
global using System;
global using MelonLoader;
global using HarmonyLib;
global using UnityEngine;
global using System.Reflection;
global using System.Collections;
global using System.Collections.Generic;
global using Il2Cpp;
global using UnityEngine.Events;
global using UnityEngine.UI;
global using static GearInfo.InfoHarvester;
global using UnityEngine.AddressableAssets;
global using UnityEngine.ResourceManagement.AsyncOperations;
using Il2CppTMPro;

namespace GearInfo
{
    internal class Utility
    {
        public const string modVersion = "0.9.1";
        public const string modName = "GearInfo";
        public const string modAuthor = "Waltz";

        public const string resourcesFolder = "GearInfo.Resources."; // root is project name

        public static Dictionary<CharacterSet, TMP_FontAsset> fontDict = new();

        public static bool IsScenePlayable()
        {
            return !(string.IsNullOrEmpty(GameManager.m_ActiveScene) || GameManager.m_ActiveScene.Contains("MainMenu") || GameManager.m_ActiveScene == "Boot" || GameManager.m_ActiveScene == "Empty");
        }

        public static bool IsScenePlayable(string scene)
        {
            return !(string.IsNullOrEmpty(scene) || scene.Contains("MainMenu") || scene == "Boot" || scene == "Empty");
        }

        public static bool IsMainMenu(string scene)
        {
            return !string.IsNullOrEmpty(scene) && scene.Contains("MainMenu");
        }

        public static void Log(ConsoleColor color, string message)
        {
            //if (Settings.options.debugLog)
            //{
                Melon<Main>.Logger.Msg(color, message);
            //}
        }

        public static AssetBundle? LoadEmbeddedAssetBundle(string name)
        {
            AssetBundle? result = null;

            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcesFolder + name);
            if (stream != null)
            {
                MemoryStream memoryStream = new MemoryStream((int)stream.Length);
                stream.CopyTo(memoryStream);
                result = AssetBundle.LoadFromMemory(memoryStream.ToArray());
            }

            return result;
        }

        public static string? LoadEmbeddedJSON(string name)
        {
            string? result = null;

            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcesFolder + name);
            if (stream != null)
            {
                StreamReader reader = new StreamReader(stream);
                result = reader.ReadToEnd();
            }

            return result;
        }

        public static bool FastApproximately(float a, float b, float threshold)
        {
            if (threshold > 0f)
            {
                return Mathf.Abs(a - b) <= threshold;
            }
            else
            {
                return Mathf.Approximately(a, b);
            }
        }

        public static TMP_FontAsset AquireFont()
        {
            CharacterSet cs = Localization.GetCharacterSet();

            if (cs != CharacterSet.Latin)
            {
                if (fontDict.TryGetValue(cs, out TMP_FontAsset fa))
                {
                    return fa;
                }
                else
                { 
                    TMP_FontAsset newFa = TMP_FontAsset.CreateFontAsset(GameManager.GetFontManager().GetUIFontForCharacterSet(cs).dynamicFont);
                    fontDict[cs] = newFa;
                    return newFa;
                }
            }
            else
            {
                return GameManager.GetFontManager().GetTMPFontForCharacterSet(cs);
            }
        }

        public static int[] GetFontSizes()
        {
            if (Localization.GetCharacterSet() == CharacterSet.Cyrillic)
            {
                return [24, 20];
            }
            else
            {
                if (int.TryParse(Localization.Get("GI_FontSizeTitle"), out int title))
                { 
                    if (int.TryParse(Localization.Get("GI_FontSizeData"), out int data))
                    {
                        return [title, data];
                    }
                }

                return [28, 24];
            }
        }
    }
}
