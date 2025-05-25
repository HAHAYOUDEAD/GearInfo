using ModSettings;

namespace GearInfo
{
    internal static class Settings
    {
        public static void OnLoad()
        {
            Settings.options = new GearInfoSettings();
        }

        public static GearInfoSettings options;
    }

    internal class GearInfoSettings : JsonModSettings
    {
        [Name("why")]
        public bool adjustForDifficulty = true;
        [Name("the")]
        public bool foodPoisoningAlt = true;
        [Name("heck")]
        public bool decayAlt = true;
        [Name("are")]
        public bool toolUseAlt = false;
        [Name("those")]
        public bool fireStartAlt = false;
        [Name("required")]
        public bool fuelInfoAlt = false;


        protected override void OnConfirm()
        {
            base.OnConfirm();
        }
    }
}
