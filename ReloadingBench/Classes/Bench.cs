using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using AreaLib;
using ReloadingBench.Enums;
using ReloadingBench.Managers;

namespace ReloadingBench.Classes
{
    [Serializable]
    public class Bench
    {
        #region Properties
        [XmlAttribute(AttributeName = "id")]
        public Guid Id { get; set; } = Guid.Empty;

        [XmlElement(ElementName = "Owner")]
        public Character Owner { get; set; } = Character.Unknown;

        [XmlElement(ElementName = "Position")]
        public Vector3 Position { get; set; } = Vector3.Zero;

        [XmlElement(ElementName = "Rotation")]
        public Vector3 Rotation { get; set; } = Vector3.Zero;

        [XmlIgnore]
        public IReadOnlyDictionary<string, int> Materials => _inventory;

        [XmlIgnore]
        public string SavePath => Path.Combine(_saveDir, $"{Id}.xml");

        [XmlIgnore]
        public Sphere Area { get; private set; }
        #endregion

        #region Fields
        private static readonly XmlSerializer _serializer = new XmlSerializer(typeof(Bench));
        private static readonly string _saveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ReloadingBenchMod");

        private Prop _bench = null;
        private readonly List<Prop> _ammoContainers = new List<Prop>();
        private Blip _blip = null;

        private Dictionary<string, int> _inventory = new Dictionary<string, int>();
        #endregion

        #region Methods
        public void CreateEntities()
        {
            if (_bench == null)
            {
                _bench = World.CreateProp("gr_prop_gr_bench_04b", Position, Rotation, false, false);

                // Ammo containers
                Prop container1 = World.CreateProp("gr_prop_gunlocker_ammo_01a", Position, false, false);
                container1.AttachTo(_bench, 0, new Vector3(0.47f, -0.07f, 0.94f), new Vector3(0.0f, 0.0f, -20.0f));

                Prop container2 = World.CreateProp("gr_prop_gr_bulletscrate_01a", Position, false, false);
                container2.AttachTo(_bench, 0, new Vector3(-0.63f, -0.08f, 0.8f), new Vector3(0.0f, 0.0f, 100.0f));

                Prop container3 = World.CreateProp("prop_box_ammo07b", Position, false, false);
                container3.AttachTo(_bench, 0, new Vector3(-0.14f, 0.04f, 0.8f), new Vector3(0.0f, 0.0f, 65.0f));

                _ammoContainers.AddRange(new Prop[] { container1, container2, container3 });
            }

            if (_blip == null)
            {
                _blip = World.CreateBlip(Position);
                _blip.Sprite = BlipSprite.WeaponSupplies;
                _blip.Scale = 0.85f;
                _blip.IsShortRange = true;
                _blip.Name = "Reloading Bench";
            }

            if (Area == null)
            {
                Area = new Sphere(_bench.GetOffsetInWorldCoords(new Vector3(0.0f, -1.0f, 0.0f)), 1.25f);
                Area.SetData("rbMod_Id", Id);

                AreaLibrary.Track(Area);
            }
        }

        public void SetBlipVisible(bool visible)
        {
            if (_blip != null)
            {
                Function.Call(Hash.SET_BLIP_DISPLAY, _blip.Handle, visible ? 2 : 0);
            }
        }

        public void RemoveEntities()
        {
            // Remove props
            if (_bench != null)
            {
                _bench.Delete();
                _bench = null;
            }

            foreach (Prop prop in _ammoContainers)
            {
                prop.Delete();
            }

            // Remove the blip
            if (_blip != null)
            {
                _blip.Remove();
                _blip = null;
            }

            _ammoContainers.Clear();
        }
        #endregion

        #region Inventory methods
        public void LoadInventory()
        {
            if (!File.Exists(SavePath))
            {
                return;
            }

            XDocument doc = XDocument.Load(SavePath);
            _inventory = doc.Descendants("Item").ToDictionary(
                item => item.Attribute("materialId").Value,
                item => Convert.ToInt32(item.Attribute("amount").Value)
            );
        }

        public void SaveInventory()
        {
            Directory.CreateDirectory(_saveDir);

            XDocument doc = new XDocument(
                new XElement("BenchInventory", _inventory.Select(
                    kv => new XElement("Item", new XAttribute[] { new XAttribute("materialId", kv.Key), new XAttribute("amount", kv.Value) })
                ))
            );

            doc.Save(SavePath);
        }

        public void SetMaterialAmount(string materialId, int amount)
        {
            if (!MaterialManager.IsValid(materialId))
            {
                throw new ArgumentException($"Invalid material: {materialId}", nameof(materialId));
            }

            _inventory[materialId] = amount;
        }

        public void ChangeMaterialAmount(string materialId, int amount)
        {
            if (!MaterialManager.IsValid(materialId))
            {
                throw new ArgumentException($"Invalid material: {materialId}", nameof(materialId));
            }

            if (!HasMaterial(materialId))
            {
                _inventory[materialId] = amount;
            }
            else
            {
                _inventory[materialId] += amount;
            }
        }

        public int GetMaterialAmount(string materialId)
        {
            if (!MaterialManager.IsValid(materialId))
            {
                throw new ArgumentException($"Invalid material: {materialId}", nameof(materialId));
            }

            return HasMaterial(materialId) ? _inventory[materialId] : 0;
        }

        public bool HasMaterial(string materialId)
        {
            return _inventory.ContainsKey(materialId);
        }

        public void ClearInventory()
        {
            _inventory.Clear();
        }
        #endregion

        #region Static methods
        public static Bench FromXElement(XElement element)
        {
            using (XmlReader reader = element.CreateReader())
            {
                Bench bench = _serializer.Deserialize(reader) as Bench;
                bench.LoadInventory();
                return bench;
            }
        }
        #endregion
    }
}
