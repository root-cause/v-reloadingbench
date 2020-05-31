using GTA;
using GTA.Native;

namespace ReloadingBench.Extensions
{
    public static class PedExtensions
    {
        private const Hash _GET_MAX_AMMO_BY_TYPE = (Hash)0x585847C5E4E11709;
        private const Hash _ADD_AMMO_TO_PED_BY_TYPE = (Hash)0x2472622CE1F2D45F;

        public static int GetAmmoByType(this Ped ped, int ammoTypeHash)
        {
            return Function.Call<int>(Hash.GET_PED_AMMO_BY_TYPE, ped.Handle, ammoTypeHash);
        }

        public static int GetMaxAmmoByType(this Ped ped, int ammoTypeHash)
        {
            int maxAmmo = 0;

            unsafe
            {
                Function.Call(_GET_MAX_AMMO_BY_TYPE, ped.Handle, ammoTypeHash, &maxAmmo);
            }

            return maxAmmo;
        }

        public static void AddAmmoByType(this Ped ped, int ammoTypeHash, int amount)
        {
            Function.Call(_ADD_AMMO_TO_PED_BY_TYPE, ped.Handle, ammoTypeHash, amount);
        }
    }
}
