using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using ReloadingBench.Classes;

namespace ReloadingBench.Managers
{
    public static class BenchManager
    {
        #region Properties
        public static IReadOnlyCollection<Bench> Benches => _data.Values;
        #endregion

        #region Fields
        private static Dictionary<Guid, Bench> _data = new Dictionary<Guid, Bench>();
        #endregion

        #region Methods
        public static void Init()
        {
            string filePath = Path.Combine("scripts", "ReloadingBenchData", "benches.xml");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("benches.xml not found, make sure you have ReloadingBenchData inside scripts.");
            }

            XDocument doc = XDocument.Load(filePath);
            _data = doc.Descendants("Bench").ToDictionary(
                item => Guid.Parse(item.Attribute("id").Value),
                item => Bench.FromXElement(item)
            );
        }

        public static bool HasBench(Guid id)
        {
            return _data.ContainsKey(id);
        }

        public static Bench GetBench(Guid id)
        {
            return HasBench(id) ? _data[id] : null;
        }

        public static void Clear()
        {
            foreach (Bench bench in _data.Values)
            {
                bench.RemoveEntities();
            }

            _data.Clear();
        }
        #endregion
    }
}
