using UnityEngine;

namespace HorrorEngine
{
    [CreateAssetMenu(fileName = "FPCamSettingsSensitivity", menuName = "Horror Engine/Settings/FPCam/Sensitivity")]
    public class FPCamSettingsSensitivity : SettingsElementSliderContent
    {
        public override void Apply()
        {
            // DO NOTHIING - this will be read directly from the settings
        }

        public override string GetDefaultValue()
        {
            return "1";
        }
    }
}
