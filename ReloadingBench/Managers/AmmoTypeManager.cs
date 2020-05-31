using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using GTA;

namespace ReloadingBench.Managers
{
    public static class AmmoTypeManager
    {
        #region Properties
        public static IReadOnlyDictionary<int, string> AmmoTypes => _data;
        #endregion

        #region Fields
        private static Dictionary<int, string> _data = new Dictionary<int, string>();
        #endregion

        #region Methods
        public static void Init()
        {
            string filePath = Path.Combine("scripts", "ReloadingBenchData", "ammoTypes.xml");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("ammoTypes.xml not found, make sure you have ReloadingBenchData inside scripts.");
            }

            XDocument doc = XDocument.Load(filePath);
            _data = doc.Descendants("Item").ToDictionary(
                item => Game.GenerateHash(item.Attribute("id").Value),
                item => item.Attribute("name").Value
            );
        }

        public static bool IsValid(int typeHash)
        {
            return _data.ContainsKey(typeHash);
        }

        public static bool IsValid(string typeName)
        {
            return IsValid(Game.GenerateHash(typeName));
        }

        public static string GetName(int typeHash)
        {
            return IsValid(typeHash) ? _data[typeHash] : typeHash.ToString();
        }

        public static int[] GetHashes()
        {
            return _data.Keys.ToArray();
        }

        public static string[] GetNames()
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
