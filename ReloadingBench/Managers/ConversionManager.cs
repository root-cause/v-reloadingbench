using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using GTA;
using ReloadingBench.Classes;

namespace ReloadingBench.Managers
{
    public static class ConversionManager
    {
        #region Properties
        public static IReadOnlyDictionary<int, Conversion> Conversions => _data;
        #endregion

        #region Fields
        private static Dictionary<int, Conversion> _data = new Dictionary<int, Conversion>();
        #endregion

        #region Methods
        public static void Init()
        {
            string filePath = Path.Combine("scripts", "ReloadingBenchData", "conversions.xml");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("conversions.xml not found, make sure you have ReloadingBenchData inside scripts.");
            }

            XDocument doc = XDocument.Load(filePath);
            _data = doc.Descendants("Conversion").ToDictionary(
                item => Game.GenerateHash(item.Attribute("ammoType").Value),
                item => Conversion.FromXElement(item)
            );
        }

        public static bool HasConversion(int ammoTypeHash)
        {
            return _data.ContainsKey(ammoTypeHash);
        }

        public static Conversion GetConversion(int ammoTypeHash)
        {
            return HasConversion(ammoTypeHash) ? _data[ammoTypeHash] : null;
        }

        public static Conversion[] GetAllConversions()
        {
            return _data.Values.ToArray();
        }

        public static void Clear()
        {
            _data.Clear();
        }
        #endregion
    }
}
