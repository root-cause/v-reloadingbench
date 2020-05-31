using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using AreaLib;
using NativeUI;
using ReloadingBench.Enums;
using ReloadingBench.Classes;
using ReloadingBench.Managers;
using ReloadingBench.Extensions;

namespace ReloadingBench
{
    public class Main : Script
    {
        public const int InputLength = 3;
        public const int CharacterUpdateInterval = 500;

        public bool GameReady = false;
        public Character CurCharacter = Character.Unknown;
        public int NextCharacterUpdate = 0;
        public ListMode CurListMode = ListMode.None;
        public Bench CurBench = null;
        public bool CanUseBench = false;
        public Random Rng = new Random();

        // Action menu variables
        public int CurAmmoType = 0;
        public int CurAmount = 0;
        public Dictionary<string, int> CurCraftingAmounts = new Dictionary<string, int>();

        // NativeUI variables
        public MenuPool MenuPool = new MenuPool();
        public UIMenu BenchMain; // Main bench menu
        public UIMenu BenchList; // Generic list menu (for ammo type selection and bench inventory display)
        public UIMenu BenchAction; // Action menu (crafting/dismantling feature works from here)

        public UIMenuItem AmountItem;
        public UIMenuColoredItem MaterialSeparator;
        public UIMenuColoredItem MaterialHeader;
        public Dictionary<string, UIMenuColoredItem> MaterialMenuItems = new Dictionary<string, UIMenuColoredItem>();
        public UIMenuColoredItem ActionSeparator;
        public UIMenuItem ConfirmItem;

        #region Constructor
        public Main()
        {
            // Initialize managers
            AmmoTypeManager.Init();
            MaterialManager.Init();
            ConversionManager.Init();
            BenchManager.Init();

            // NativeUI
            Point noBannerOffset = new Point(0, -107);
            UIResRectangle emptyBanner = new UIResRectangle(Point.Empty, Size.Empty, Color.Empty);
            
            // Main menu
            BenchMain = new UIMenu(string.Empty, "SELECT AN OPTION", noBannerOffset);
            BenchMain.SetBannerType(emptyBanner);

            // Generic list menu for displaying ammo types/bench materials, subtitle changes with BenchMain item selection
            BenchList = new UIMenu(string.Empty, "LIST MENU TITLE", noBannerOffset);
            BenchList.SetBannerType(emptyBanner);

            // Action menu, subtitle changes with BenchList item selection
            BenchAction = new UIMenu(string.Empty, "ACTION MENU TITLE", noBannerOffset);
            BenchAction.SetBannerType(emptyBanner);

            MaterialHeader = new UIMenuColoredItem("~b~MATERIAL", Color.Transparent, Color.Transparent);
            MaterialHeader.SetRightLabel("~b~BENCH");
            MaterialHeader.Enabled = false;

            // Amount button
            AmountItem = new UIMenuItem("Amount");
            AmountItem.SetRightLabel("Select...");

            // Confirm button
            ConfirmItem = new UIMenuItem("Confirm");
            ConfirmItem.SetLeftBadge(UIMenuItem.BadgeStyle.Tick);

            // Separators
            MaterialSeparator = new UIMenuColoredItem("TITLE", Color.Black, Color.Black);
            MaterialSeparator.Enabled = false;

            ActionSeparator = new UIMenuColoredItem("~b~ACTION", Color.Black, Color.Black);
            ActionSeparator.Enabled = false;

            // Menu relations
            BenchMain.BindMenuToItem(BenchList, new UIMenuItem("Craft Ammo"));
            BenchMain.BindMenuToItem(BenchList, new UIMenuItem("Dismantle Ammo"));
            BenchMain.BindMenuToItem(BenchList, new UIMenuItem("Bench Materials"));

            // Add menus to the menupool
            MenuPool.Add(BenchMain);
            MenuPool.Add(BenchList);
            MenuPool.Add(BenchAction);

            // Menu events
            BenchMain.OnItemSelect += BenchMain_OnItemSelect;
            BenchList.OnItemSelect += BenchList_OnItemSelect;
            AmountItem.Activated += AmountItem_Activated;
            ConfirmItem.Activated += ConfirmItem_Activated;

            // Events
            Tick += Main_Tick;
            Aborted += Main_Aborted;
        }
        #endregion

        #region Script Events
        private void Main_Tick(object sender, EventArgs e)
        {
            // Set up props and areas when the game is ready
            if (!GameReady && !Game.IsLoading && Game.Player.CanControlCharacter)
            {
                GameReady = true;

                CurCharacter = Util.GetCharacterFromModel(Game.Player.Character.Model.Hash);
                NextCharacterUpdate = Game.GameTime + CharacterUpdateInterval;

                foreach (Bench bench in BenchManager.Benches)
                {
                    bench.CreateEntities();
                    bench.SetBlipVisible(bench.Owner == Character.Unknown || bench.Owner == CurCharacter);

                    bench.Area.PlayerEnter += PlayerEnterBenchArea;
                    bench.Area.PlayerLeave += PlayerLeaveBenchArea;
                }
            }

            int gameTime = Game.GameTime;

            // Hide blips of benches the player can't interact with
            if (gameTime >= NextCharacterUpdate)
            {
                NextCharacterUpdate = gameTime + CharacterUpdateInterval;

                Character newCharacter = Util.GetCharacterFromModel(Game.Player.Character.Model.Hash);
                if (CurCharacter != newCharacter)
                {
                    CurCharacter = newCharacter;

                    foreach (Bench bench in BenchManager.Benches)
                    {
                        bench.SetBlipVisible(bench.Owner == Character.Unknown || bench.Owner == CurCharacter);
                    }
                }
            }

            // Interaction prompt
            if (CurBench != null && !MenuPool.IsAnyMenuOpen())
            {
                if (CanUseBench)
                {
                    Util.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to use the reloading bench.");

                    if (Game.IsControlJustPressed(0, Control.Context))
                    {
                        BenchMain.RefreshIndex();
                        BenchMain.Visible = true;
                    }
                }
                else
                {
                    Util.DisplayHelpTextThisFrame("You can't use this reloading bench.");
                }
            }

            // Handle menus
            MenuPool.ProcessMenus();
        }

        private void Main_Aborted(object sender, EventArgs e)
        {
            // Unsub NativeUI events
            if (BenchMain != null)
            {
                BenchMain.OnItemSelect -= BenchMain_OnItemSelect;
            }

            if (BenchList != null)
            {
                BenchList.OnItemSelect -= BenchList_OnItemSelect;
            }

            if (AmountItem != null)
            {
                AmountItem.Activated -= AmountItem_Activated;
            }

            if (ConfirmItem != null)
            {
                ConfirmItem.Activated -= ConfirmItem_Activated;
            }

            // Clear managers
            AmmoTypeManager.Clear();
            MaterialManager.Clear();
            ConversionManager.Clear();

            // Unsub area events before nuking benches
            foreach (Bench bench in BenchManager.Benches)
            {
                if (bench.Area != null)
                {
                    bench.Area.PlayerEnter -= PlayerEnterBenchArea;
                    bench.Area.PlayerLeave -= PlayerLeaveBenchArea;
                }
            }

            BenchManager.Clear();

            // Other clean-up
            CurCraftingAmounts.Clear();
            MaterialMenuItems.Clear();

            CurCraftingAmounts = null;
            MaterialMenuItems = null;
        }
        #endregion

        #region AreaLib Events
        private void PlayerEnterBenchArea(AreaBase area)
        {
            if (area.GetData("rbMod_Id", out Guid benchId))
            {
                CurBench = BenchManager.GetBench(benchId);
                CanUseBench = CurBench == null ? false : (CurBench.Owner == Character.Unknown || CurBench.Owner == Util.GetCharacterFromModel(Game.Player.Character.Model.Hash));
            }
        }

        private void PlayerLeaveBenchArea(AreaBase area)
        {
            CurBench = null;
            CanUseBench = false;

            MenuPool.CloseAllMenus();
            CurCraftingAmounts.Clear();
            MaterialMenuItems.Clear();
        }
        #endregion

        #region NativeUI Events
        private void BenchMain_OnItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (CurBench == null || !CanUseBench)
            {
                return;
            }

            BenchList.RefreshIndex();
            BenchList.Clear();

            switch (index)
            {
                case 0: // List ammo types for crafting
                    BenchList.Subtitle.Caption = "~b~CRAFTING: ~s~SELECT AMMO";
                    CurListMode = ListMode.Crafting;

                    foreach (var ammoType in AmmoTypeManager.AmmoTypes)
                    {
                        UIMenuItem item = new UIMenuItem(ammoType.Value);
                        Conversion conversion = ConversionManager.GetConversion(ammoType.Key);

                        if (conversion == null)
                        {
                            item.Description = "Ammo type doesn't have conversion data.";
                            item.Enabled = false;
                        }
                        else if (conversion.CraftingMaterials.Count == 0)
                        {
                            item.Description = "Ammo type doesn't have crafting data.";
                            item.Enabled = false;
                        }

                        BenchList.BindMenuToItem(BenchAction, item);
                    }

                    break;

                case 1: // List ammo types for dismantling
                    BenchList.Subtitle.Caption = "~b~DISMANTLING: ~s~SELECT AMMO";
                    CurListMode = ListMode.Dismantling;

                    foreach (var ammoType in AmmoTypeManager.AmmoTypes)
                    {
                        UIMenuItem item = new UIMenuItem(ammoType.Value);
                        Conversion conversion = ConversionManager.GetConversion(ammoType.Key);

                        if (conversion == null)
                        {
                            item.Description = "Ammo type doesn't have conversion data.";
                            item.Enabled = false;
                        }
                        else if (conversion.BreakdownMaterials.Count == 0)
                        {
                            item.Description = "Ammo type doesn't have dismantling data.";
                            item.Enabled = false;
                        }

                        BenchList.BindMenuToItem(BenchAction, item);
                    }

                    break;

                case 2: // Inventory
                    BenchList.Subtitle.Caption = "BENCH INVENTORY";
                    CurListMode = ListMode.Inventory;

                    foreach (var material in MaterialManager.Materials)
                    {
                        UIMenuItem item = new UIMenuItem(material.Value);
                        item.SetRightLabel(CurBench.GetMaterialAmount(material.Key).ToString());

                        BenchList.AddItem(item);
                    }

                    break;
            }
        }

        private void BenchList_OnItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (CurBench == null || !CanUseBench || CurListMode == ListMode.None || CurListMode == ListMode.Inventory)
            {
                return;
            }

            CurAmmoType = AmmoTypeManager.GetHashes()[index];
            CurAmount = 0;

            BenchAction.RefreshIndex();
            BenchAction.Clear();

            CurCraftingAmounts.Clear();
            MaterialMenuItems.Clear();

            switch (CurListMode)
            {
                case ListMode.Crafting:
                    BenchAction.Subtitle.Caption = $"~b~CRAFTING: ~s~{selectedItem.Text.ToUpperInvariant()}";
                    MaterialSeparator.Text = "~b~REQUIRES";

                    bool isMaxCapacity = Util.CalcAmmoCapacity(CurAmmoType) == 0;
                    AmountItem.SetRightLabel(isMaxCapacity ? "Full" : "Select...");
                    BenchAction.AddItem(AmountItem);
                    BenchAction.AddItem(MaterialSeparator);
                    BenchAction.AddItem(MaterialHeader);

                    // Crafting material display
                    foreach (var material in ConversionManager.Conversions[CurAmmoType].CraftingMaterials)
                    {
                        UIMenuColoredItem item = new UIMenuColoredItem($"→ {material.Value} {MaterialManager.GetName(material.Key)}", Color.Transparent, Color.Transparent);
                        item.SetRightLabel(CurBench.GetMaterialAmount(material.Key).ToString());
                        item.HighlightedTextColor = Color.WhiteSmoke;
                        item.Enabled = false;

                        MaterialMenuItems[material.Key] = item;
                        BenchAction.AddItem(item);
                    }

                    BenchAction.AddItem(ActionSeparator);
                    BenchAction.AddItem(ConfirmItem);

                    AmountItem.Enabled = !isMaxCapacity;
                    ConfirmItem.Enabled = false;
                    break;

                case ListMode.Dismantling:
                    BenchAction.Subtitle.Caption = $"~b~DISMANTLING: ~s~{selectedItem.Text.ToUpperInvariant()}";
                    MaterialSeparator.Text = "~b~GIVES";

                    bool canDismantle = Game.Player.Character.GetAmmoByType(CurAmmoType) > 0;
                    AmountItem.SetRightLabel(canDismantle ? "Select..." : "No Ammo");
                    BenchAction.AddItem(AmountItem);
                    BenchAction.AddItem(MaterialSeparator);
                    BenchAction.AddItem(MaterialHeader);

                    // Dismantling material display
                    foreach (var material in ConversionManager.Conversions[CurAmmoType].BreakdownMaterials)
                    {
                        int min = material.Value.Item1;
                        int max = material.Value.Item2;

                        UIMenuColoredItem item = new UIMenuColoredItem($"→ {(min == max ? max.ToString() : $"{min}-{max}")} {MaterialManager.GetName(material.Key)}", Color.Transparent, Color.Transparent);
                        item.SetRightLabel(CurBench.GetMaterialAmount(material.Key).ToString());
                        item.HighlightedTextColor = Color.WhiteSmoke;

                        MaterialMenuItems[material.Key] = item;
                        BenchAction.AddItem(item);
                    }

                    BenchAction.AddItem(ActionSeparator);
                    BenchAction.AddItem(ConfirmItem);

                    AmountItem.Enabled = canDismantle;
                    ConfirmItem.Enabled = false;
                    break;
            }
        }

        private void AmountItem_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            if (CurBench == null || !CanUseBench)
            {
                return;
            }

            switch (CurListMode)
            {
                case ListMode.Crafting:
                {
                    if (int.TryParse(Game.GetUserInput(InputLength), out int amount) && amount > 0)
                    {
                        int max = Util.CalcAmmoCapacity(CurAmmoType);

                        // Cap input to the player's ammo capacity
                        if (amount > max)
                        {
                            amount = max;
                        }

                        CurAmount = amount;
                        CurCraftingAmounts.Clear();

                        AmountItem.SetRightLabel(CurAmount.ToString());

                        // Update material display
                        foreach (var material in ConversionManager.Conversions[CurAmmoType].CraftingMaterials)
                        {
                            int requiredAmount = material.Value * CurAmount;

                            MaterialMenuItems[material.Key].Text = $"→ {requiredAmount} {MaterialManager.GetName(material.Key)}";
                            MaterialMenuItems[material.Key].Enabled = CurBench.GetMaterialAmount(material.Key) >= requiredAmount;

                            CurCraftingAmounts[material.Key] = requiredAmount;
                        }

                        ConfirmItem.Enabled = MaterialMenuItems.Values.All(item => item.Enabled);
                    }
                    else
                    {
                        UI.Notify("Invalid amount specified.");
                    }

                    break;
                }

                case ListMode.Dismantling:
                {
                    if (int.TryParse(Game.GetUserInput(InputLength), out int amount) && amount > 0)
                    {
                        int max = Game.Player.Character.GetAmmoByType(CurAmmoType);

                        // Cap input to the amount of ammo (CurAmmoType) the player has
                        if (amount > max)
                        {
                            amount = max;
                        }

                        CurAmount = amount;
                        AmountItem.SetRightLabel(CurAmount.ToString());

                        // Update material display
                        foreach (var material in ConversionManager.Conversions[CurAmmoType].BreakdownMaterials)
                        {
                            int recMin = material.Value.Item1 * CurAmount;
                            int recMax = material.Value.Item2 * CurAmount;

                            MaterialMenuItems[material.Key].Text = $"→ {(recMin == recMax ? recMax.ToString() : $"{recMin}-{recMax}")} {MaterialManager.GetName(material.Key)}";
                        }

                        ConfirmItem.Enabled = true;
                    }
                    else
                    {
                        UI.Notify("Invalid amount specified.");
                    }

                    break;
                }
            }
        }

        private void ConfirmItem_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            if (CurBench == null || !CanUseBench)
            {
                return;
            }

            switch (CurListMode)
            {
                case ListMode.Crafting:
                {
                    int playerAmmo = Game.Player.Character.GetAmmoByType(CurAmmoType);
                    int playerMax = Game.Player.Character.GetMaxAmmoByType(CurAmmoType);
                    if (playerAmmo + CurAmount > playerMax)
                    {
                        UI.Notify($"Crafting {CurAmount} {AmmoTypeManager.GetName(CurAmmoType)} exceeds your carrying limit. ({playerMax})");
                        return;
                    }

                    bool hasAllMats = CurCraftingAmounts.All(mat => CurBench.GetMaterialAmount(mat.Key) >= mat.Value);
                    if (!hasAllMats)
                    {
                        UI.Notify($"You don't have enough materials to craft {CurAmount} {AmmoTypeManager.GetName(CurAmmoType)}.");
                        return;
                    }

                    // Update & save bench materials
                    foreach (var material in CurCraftingAmounts)
                    {
                        CurBench.ChangeMaterialAmount(material.Key, -material.Value);

                        int newMatAmount = CurBench.GetMaterialAmount(material.Key);
                        MaterialMenuItems[material.Key].SetRightLabel(newMatAmount.ToString());
                        MaterialMenuItems[material.Key].Enabled = newMatAmount >= CurCraftingAmounts[material.Key];
                    }

                    CurBench.SaveInventory();

                    // Sound fx
                    Util.PlaySoundFrontend("HUD_FRONTEND_CUSTOM_SOUNDSET", "PICK_UP_WEAPON");

                    Game.Player.Character.AddAmmoByType(CurAmmoType, CurAmount);
                    UI.Notify($"Crafted {CurAmount} {AmmoTypeManager.GetName(CurAmmoType)}.");

                    // Check for ammo limit and material amount
                    playerAmmo = Game.Player.Character.GetAmmoByType(CurAmmoType);
                    if (playerAmmo >= playerMax)
                    {
                        BenchAction.RefreshIndex();
                        AmountItem.SetRightLabel("Full");

                        AmountItem.Enabled = false;
                        ConfirmItem.Enabled = false;
                    }
                    else if (playerAmmo + CurAmount > playerMax)
                    {
                        BenchAction.RefreshIndex();
                        AmountItem.SetRightLabel("Select...");

                        ConfirmItem.Enabled = false;
                    }
                    else if (MaterialMenuItems.Values.Any(item => !item.Enabled))
                    {
                        BenchAction.RefreshIndex();
                        AmountItem.SetRightLabel("Select...");

                        ConfirmItem.Enabled = false;
                    }

                    break;
                }

                case ListMode.Dismantling:
                {
                    int playerAmmo = Game.Player.Character.GetAmmoByType(CurAmmoType);
                    if (playerAmmo < CurAmount)
                    {
                        UI.Notify($"You don't have {CurAmount} {AmmoTypeManager.GetName(CurAmmoType)}.");
                        return;
                    }

                    // Take ammo from the player
                    Game.Player.Character.AddAmmoByType(CurAmmoType, -CurAmount);

                    // Update & save bench materials
                    string notification = $"Dismantled {CurAmount} {AmmoTypeManager.GetName(CurAmmoType)} for:~n~";

                    foreach (var material in ConversionManager.Conversions[CurAmmoType].BreakdownMaterials)
                    {
                        int matAmount = Rng.Next(material.Value.Item1 * CurAmount, material.Value.Item2 * CurAmount);
                        CurBench.ChangeMaterialAmount(material.Key, matAmount);

                        MaterialMenuItems[material.Key].SetRightLabel(CurBench.GetMaterialAmount(material.Key).ToString());

                        notification += $"- {matAmount} {MaterialManager.GetName(material.Key)}~n~";
                    }

                    UI.Notify(notification);
                    CurBench.SaveInventory();

                    // Sound fx
                    Util.PlaySoundFrontend("HUD_FRONTEND_CUSTOM_SOUNDSET", "PICK_UP_WEAPON");

                    // Disable confirmation button because player has less ammo than CurAmount
                    playerAmmo = Game.Player.Character.GetAmmoByType(CurAmmoType);
                    if (playerAmmo < CurAmount)
                    {
                        BenchAction.RefreshIndex();

                        if (playerAmmo == 0)
                        {
                            AmountItem.SetRightLabel("No Ammo");
                            AmountItem.Enabled = false;
                        }
                        else
                        {
                            AmountItem.SetRightLabel("Select...");
                        }

                        ConfirmItem.Enabled = false;
                    }

                    break;
                }
            }
        }
        #endregion
    }
}
