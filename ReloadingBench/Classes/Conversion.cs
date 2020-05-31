using System;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections.Generic;
using GTA;
using ReloadingBench.Managers;

namespace ReloadingBench.Classes
{
    public class Conversion
    {
        #region Properties
        public int AmmoType { get; private set; }
        public IReadOnlyDictionary<string, Tuple<int, int>> BreakdownMaterials => _breakdownMats;
        public IReadOnlyDictionary<string, int> CraftingMaterials => _craftingMats;
        #endregion

        #region Fields
        private readonly Dictionary<string, Tuple<int, int>> _breakdownMats = new Dictionary<string, Tuple<int, int>>();
        private readonly Dictionary<string, int> _craftingMats = new Dictionary<string, int>();
        #endregion

        #region Constructor
        private Conversion() { }
        #endregion

        #region Methods
        public void RegisterBreakdownMaterial(string materialId, int minAmount, int maxAmount)
        {
            if (!MaterialManager.IsValid(materialId))
            {
                throw new ArgumentException($"Invalid material: {materialId}", nameof(materialId));
            }
            else if (minAmount < 0)
            {
                throw new ArgumentOutOfRangeException("min", "min can't be less than 0.");
            }
            else if (maxAmount < 0)
            {
                throw new ArgumentOutOfRangeException("max", "max can't be less than 0.");
            }

            _breakdownMats[materialId] = Tuple.Create(Math.Min(minAmount, maxAmount), Math.Max(minAmount, maxAmount));
        }

        public void RegisterCraftingMaterial(string materialId, int amount)
        {
            if (!MaterialManager.IsValid(materialId))
            {
                throw new ArgumentException($"Invalid material: {materialId}", nameof(materialId));
            }
            else if (amount < 0)
            {
                throw new ArgumentOutOfRangeException("amount", "amount can't be less than 0.");
            }

            _craftingMats[materialId] = amount;
        }
        #endregion

        #region Static methods
        public static Conversion FromXElement(XElement element)
        {
            Conversion conversion = new Conversion { AmmoType = Game.GenerateHash(element.Attribute("ammoType").Value) };

            // Breakdown materials
            foreach (XElement material in element.XPathSelectElements("BreakDownMaterials/Material"))
            {
                conversion.RegisterBreakdownMaterial(
                    material.Attribute("id").Value,
                    Convert.ToInt32(material.Attribute("min").Value),
                    Convert.ToInt32(material.Attribute("max").Value)
                );
            }

            // Crafting materials
            foreach (XElement material in element.XPathSelectElements("CraftingMaterials/Material"))
            {
                conversion.RegisterCraftingMaterial(material.Attribute("id").Value, Convert.ToInt32(material.Attribute("amount").Value));
            }

            return conversion;
        }
        #endregion
    }
}
