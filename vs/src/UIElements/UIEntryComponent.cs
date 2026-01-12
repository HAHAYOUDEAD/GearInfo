
using Il2CppTMPro;
using Il2CppInterop.Runtime;
using Il2CppSystem.Net;
using static GearInfo.InfoHarvester;
using Il2CppInterop.Runtime.Attributes;

namespace GearInfo
{
    [RegisterTypeInIl2Cpp]
    internal class GearInfoUIEntry : MonoBehaviour
    {
        public GearInfoUIEntry(IntPtr intPtr) : base(intPtr) { }

        public TMP_Text[] fields;
        public Button buttonCopy;
        public Button buttonSwitch;

        //public bool showingAltInfo;

        public AltInfoType altInfoType = AltInfoType.None;

        void Awake()
        {
            buttonCopy = this.transform.Find("ButtonCopy").GetComponent<Button>();
            buttonSwitch = this.transform.Find("ButtonSwitch").GetComponent<Button>();
            fields = GetComponentsInChildren<TMP_Text>(true);

            //foreach (Image i in this.transform.Find("Separator").GetComponentsInChildren<Image>()) i.color = Control.separatorColor;
        }

        void OnEnable()
        {
            int[] fontSizes = GetFontSizes();
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].font = AquireFont();
                if (i % 2 == 0) fields[i].fontSize = fontSizes[0];// even
                else fields[i].fontSize = fontSizes[1];// odd
            }

            buttonCopy.gameObject.SetActive(false);
            buttonSwitch.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            ClearListeners();
        }

        void OnDestroy()
        {
            ClearListeners();
        }

        public void ClearListeners()
        {
            buttonCopy?.onClick.RemoveAllListeners();
            buttonSwitch?.onClick.RemoveAllListeners();
        }

        [HideFromIl2Cpp]
        public void SetFields(string[] entries, AltInfoType ait = AltInfoType.None)
        {
            
            for (int i = 0; i < fields.Length; i++)
            {
                if (!string.IsNullOrEmpty(entries[i]))
                {
                    fields[i].text = entries[i];
                }
            }

            if (ait != AltInfoType.None) altInfoType = ait;
        }

        [HideFromIl2Cpp]
        public void SetButton(ButtonType buttonType, Action a)
        {
            switch (buttonType)
            {
                default:
                    break;
                case ButtonType.Copy:
                    buttonSwitch.gameObject.SetActive(false);
                    buttonCopy.gameObject.SetActive(true);
                    buttonCopy.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(a));
                    break;
                case ButtonType.Switch:
                    buttonCopy.gameObject.SetActive(false);
                    buttonSwitch.gameObject.SetActive(true);
                    buttonSwitch.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(a));
                    break;
            }
        }
    }
}
