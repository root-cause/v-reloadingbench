using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace ReloadingBench.Managers
{
    public static class MaterialManager
    {
        #region Properties
        public static IReadOnlyDictionary<string, string> Materials => _data;
        #endregion

        #region Fields
        private static Dictionary<string, string> _data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region Methods
        public static void Init()
        {
            string filePath = Path.Combine("scripts", "ReloadingBenchData", "materials.xml");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("materials.xml not found, make sure you have ReloadingBenchData inside scripts.");
            }

            XDocument doc = XDocument.Load(filePath);
            _data = doc.Descendants("Material").ToDictionary(
                item => item.Attribute("id").Value,
                item => item.Attribute("name").Value
            );
        }

        public static bool IsValid(string materialId)
        {
            return _data.ContainsKey(materialId);
        }

        public static string GetName(string materialId)
        {
            return IsValid(materialId) ? _data[materialId] : materialId;
        }

        public static string[] GetIds()
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
