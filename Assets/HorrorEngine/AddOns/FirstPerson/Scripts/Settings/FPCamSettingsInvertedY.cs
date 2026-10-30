using UnityEngine;

namespace HorrorEngine
{
    [CreateAssetMenu(fileName = "FPCamSettingsInvertedY", menuName = "Horror Engine/Settings/FPCam/Inverted Y")]
    public class FPCamSettingsInvertedY : SettingsElementToggleContent
    {
        public override void Apply()
        {
            // DO NOTHING - This will be checked from the settings
        }

        public override string GetDefaultValue()
        {
            return "0";
        }
    }
}