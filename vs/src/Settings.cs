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
        public bool temp1 = true;
        [Name("those")]
        public bool temp2 = true;
        [Name("required")]
        public bool temp3 = true;


        protected override void OnConfirm()
        {
            base.OnConfirm();
        }
    }
}
