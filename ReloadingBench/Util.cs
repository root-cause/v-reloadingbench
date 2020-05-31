using GTA;
using GTA.Native;
using ReloadingBench.Enums;
using ReloadingBench.Extensions;

namespace ReloadingBench
{
    public static class Util
    {
        public static void DisplayHelpTextThisFrame(string message)
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, message);
            Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, 0, 1, -1);
        }

        public static Character GetCharacterFromModel(int model)
        {
            switch ((PedHash)model)
            {
                case PedHash.Michael:
                    return Character.Michael;

                case PedHash.Franklin:
                    return Character.Franklin;

                case PedHash.Trevor:
                    return Character.Trevor;

                default:
                    return Character.Unknown;
            }
        }

        public static int CalcAmmoCapacity(int ammoType)
        {
            return Game.Player.Character.GetMaxAmmoByType(ammoType) - Game.Player.Character.GetAmmoByType(ammoType);
        }

        public static void PlaySoundFrontend(string audioRef, string audioName)
        {
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, audioName, audioRef, false);
        }
    }
}
