using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region in-game
        class Category {
            public string Type { get; }
            public string Tag { get; set; }
            public MyFixedPoint CurrentAmount { get; set; }
            public List<IMyTerminalBlock> Inventories { get; }

            public Category(string type)
            {
                Type = type;
                Inventories = new List<IMyTerminalBlock>();
            }
        }

        class InventoryManager
        {
            Program parent;

            MyIni ini = new MyIni();

            public Dictionary<string, Category> categories { get; } = new Dictionary<string, Category>();
            List<IMyInventory> miscInventories = new List<IMyInventory>();

            // lookups
            Dictionary<string, string> typeToCategory = new Dictionary<string, string>();
            Dictionary<string, string> tagToCategory = new Dictionary<string, string>();
            // todo: make sure rebuilding this lookup every time isn't taking too long
            Dictionary<long, List<string>> idToCategories = new Dictionary<long, List<string>>();

            // holds all relevant blocks for each sorting run
            List<IMyTerminalBlock> inventoryBlocks;

            // reusable list
            // todo: move other variables here to avoid runtime allocation
            List<MyInventoryItem> items = new List<MyInventoryItem>();

            public InventoryManager(Program parent)
            {
                this.parent = parent;
                inventoryBlocks = new List<IMyTerminalBlock>();

                // object types
                categories.Add("Ore", new Category("MyObjectBuilder_Ore"));
                categories.Add("Ingot", new Category("MyObjectBuilder_Ingot"));
                categories.Add("Component", new Category("MyObjectBuilder_Component"));
                categories.Add("Ammo", new Category("MyObjectBuilder_Ammo"));
                
                // ores
                categories.Add("Stone", new Category("MyObjectBuilder_Ore/Stone"));
                categories.Add("Scrap Metal", new Category("MyObjectBuilder_Ore/Scrap"));
                categories.Add("Ice", new Category("MyObjectBuilder_Ore/Ice"));
                categories.Add("Iron Ore", new Category("MyObjectBuilder_Ore/Iron"));
                categories.Add("Nickel Ore", new Category("MyObjectBuilder_Ore/Nickel"));
                categories.Add("Silicon Ore", new Category("MyObjectBuilder_Ore/Silicon"));
                categories.Add("Cobalt Ore", new Category("MyObjectBuilder_Ore/Cobalt"));
                categories.Add("Magnesium Ore", new Category("MyObjectBuilder_Ore/Magnesium"));
                categories.Add("Silver Ore", new Category("MyObjectBuilder_Ore/Silver"));
                categories.Add("Gold Ore", new Category("MyObjectBuilder_Ore/Gold"));
                categories.Add("Uranium Ore", new Category("MyObjectBuilder_Ore/Uranium"));
                categories.Add("Platinum Ore", new Category("MyObjectBuilder_Ore/Platinum"));

                // idk what gravel is
                categories.Add("Gravel", new Category("MyObjectBuilder_Ingot/Gravel"));

                // ingots
                categories.Add("Iron Ingot", new Category("MyObjectBuilder_Ingot/Iron"));
                categories.Add("Nickel Ingot", new Category("MyObjectBuilder_Ingot/Nickel"));
                categories.Add("Silicon Wafer", new Category("MyObjectBuilder_Ingot/Silicon"));
                categories.Add("Cobalt Ingot", new Category("MyObjectBuilder_Ingot/Cobalt"));
                categories.Add("Magnesium Powder", new Category("MyObjectBuilder_Ingot/Magnesium"));
                categories.Add("Silver Ingot", new Category("MyObjectBuilder_Ingot/Silver"));
                categories.Add("Gold Ingot", new Category("MyObjectBuilder_Ingot/Gold"));
                categories.Add("Uranium Ingot", new Category("MyObjectBuilder_Ingot/Uranium"));
                categories.Add("Platinum Ingot", new Category("MyObjectBuilder_Ingot/Platinum"));

                // get tags
                LoadIni();
                SaveIni();

                foreach (string name in categories.Keys)
                {
                    Category category = categories[name];
                    typeToCategory.Add(category.Type, name);
                    tagToCategory.Add(category.Tag, name);
                }
            }

            public void LoadIni()
            {
                /*
                 * loading tags from custom data
                 */

                if (ini.TryParse(parent.Me.CustomData))
                {
                    foreach (string name in categories.Keys)
                    {
                        string iniName = name.ToLower().Replace(' ', '_');
                        categories[name].Tag = ini.Get("general", $"{iniName}_tag").ToString($"[{name.ToLower()}]");
                    }
                }
                else // invalid/missing ini
                {
                    foreach (string name in categories.Keys)
                    {
                        categories[name].Tag = $"[{name.ToLower()}]";
                    }
                }
            }

            public void SaveIni()
            {
                /*
                 * saving tags to custom data
                 */

                // todo: split these up into tag groups

                foreach (string name in categories.Keys)
                {
                    string iniName = name.ToLower().Replace(' ', '_');
                    ini.Set("general", $"{iniName}_tag", categories[name].Tag);
                }

                // i wish i could put this above the loop (since that's where it shows in-game)
                // but MyIni will get very unhappy if the section doesn't exist
                // and apparently adding a section without a key-value pair just isn't a thing
                ini.SetSectionComment("general", "tag must be included in block name\n" +
                        "untagged inventories will be used for misc. storage\n" +
                        "recompile to update tags");

                parent.Me.CustomData = ini.ToString();
            }

            public void GetInventories()
            {
                /*
                 * grabs all blocks with inventories on the grid (except assemblers, reactors, etc.)
                 * adds blocks to different sorting categories (potentially multiple)
                 */

                inventoryBlocks.Clear();
                foreach (Category category in categories.Values)
                {
                    category.CurrentAmount = 0;
                    category.Inventories.Clear();
                }

                miscInventories.Clear();
                idToCategories.Clear();

                parent.GridTerminalSystem.GetBlocksOfType(inventoryBlocks, b => b.HasInventory);

                foreach (IMyTerminalBlock block in inventoryBlocks)
                {
                    if (block.HasInventory && !((block is IMyProductionBlock)
                        || (block is IMyPowerProducer) || (block is IMyGasGenerator)))
                    {
                        idToCategories.Add(block.EntityId, new List<string>());
                        int categoryCount = 0;

                        foreach (string name in categories.Keys)
                        {
                            Category category = categories[name];
                            if (block.CustomName.Contains(category.Tag))
                            {
                                category.Inventories.Add(block);
                                idToCategories[block.EntityId].Add(name);
                                categoryCount++;
                            }
                        }
                        if (categoryCount == 0)
                        {
                            miscInventories.Add(block.GetInventory());
                        }
                    }
                }
            }
            
            public void SortItems()
            {
                /*
                 * push items towards the 'best' inventory
                 */

                foreach (IMyTerminalBlock block in inventoryBlocks)
                {
                    items.Clear();
                    block.GetInventory().GetItems(items);
                    
                    foreach (MyInventoryItem item in items)
                    {
                        List<string> blockCategories = idToCategories[block.EntityId];

                        // it might be more accurate to call this 'full type'
                        // i'm not using the actual subtype because it would cause conflicts between ingots/ores
                        string subtype = item.Type.ToString();
                        string type = item.Type.TypeId;

                        string specificCategory = typeToCategory[subtype];
                        string generalCategory = typeToCategory[type];

                        // some quick counting
                        // technically this is not very accurate as some items may be double-counted when they are moved
                        // but, items should only move once (or possibly twice with a very fully system)
                        // so the count should be accurate after the items are sorted

                        categories[specificCategory].CurrentAmount += item.Amount;
                        categories[generalCategory].CurrentAmount += item.Amount;

                        if (categories[specificCategory].Inventories.Count > 0)
                        {
                            if (!blockCategories.Contains(specificCategory))
                            {
                                if (!MoveToTarget(block.GetInventory(), categories[specificCategory], item))
                                {
                                    if (blockCategories.Count > 0)
                                    {
                                        MoveToMisc(block.GetInventory(), item);
                                    }
                                }
                            }
                        }
                        else if (categories[generalCategory].Inventories.Count > 0)
                        {
                            if (!blockCategories.Contains(generalCategory))
                            {
                                if (!MoveToTarget(block.GetInventory(), categories[generalCategory], item))
                                {
                                    if (blockCategories.Count > 0)
                                    {
                                        MoveToMisc(block.GetInventory(), item);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // this and above is to prevent pushing from misc. storage to misc. storage
                            if (blockCategories.Count > 0)
                            {
                                MoveToMisc(block.GetInventory(), item);
                            }
                        }
                    }
                }   
            }

            private bool MoveToTarget(IMyInventory source, Category target, MyInventoryItem item)
            {
                /*
                 * try to move the item from the source inventory to an inventory of the appropriate category
                 */

                foreach (IMyTerminalBlock block in target.Inventories)
                {
                    IMyInventory destination = block.GetInventory();

                    if (source.CanTransferItemTo(destination, item.Type)
                            && destination.CanItemsBeAdded(item.Amount, item.Type))
                    {
                        if (source.TransferItemTo(destination, item))
                            return true;
                    }
                }
                return false;
            }

            private void MoveToMisc(IMyInventory source, MyInventoryItem item)
            {
                /*
                 * try to move the item to an uncategorized inventory
                 */



                foreach (IMyInventory destination in miscInventories)
                {
                    if (source.CanTransferItemTo(destination, item.Type)
                        && destination.CanItemsBeAdded(item.Amount, item.Type))
                    {
                        if (source.TransferItemFrom(destination, item))
                        {
                            return;
                        }
                    }
                }
            }
        }

        class Output
        {
            Program parent;
            InventoryManager inventoryManager;

            // reusable lists
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> panels = new List<IMyTerminalBlock>();

            // terminal i/o
            IMyTextSurface pBlockSurface;
            RectangleF pBlockViewRect = new RectangleF();

            IMyTextPanel outPanel;
            RectangleF outViewRect = new RectangleF();

            int outputRows = 5;
            int outputColumns = 2;

            public Output(Program parent, InventoryManager inventoryManager)
            {
                this.parent = parent;
                this.inventoryManager = inventoryManager;

                pBlockSurface = parent.Me.GetSurface(0);
                pBlockSurface.ContentType = ContentType.SCRIPT;
                pBlockSurface.Script = "";
                pBlockViewRect = new RectangleF((pBlockSurface.TextureSize - pBlockSurface.SurfaceSize) / 2f, pBlockSurface.SurfaceSize);

                parent.GridTerminalSystem.SearchBlocksOfName("[out]", blocks, b => b is IMyTextPanel);
                if (blocks.Count > 0)
                {
                    outPanel = (blocks[0] as IMyTextPanel);
                    outPanel.ContentType = ContentType.SCRIPT;
                    outPanel.Script = "";
                    outViewRect = new RectangleF((outPanel.TextureSize - outPanel.SurfaceSize) / 2f, outPanel.SurfaceSize);
                }
            }

            public void ShowOutput()
            {
                var pBlockFrame = pBlockSurface.DrawFrame();
                DisplayInventory(ref pBlockFrame, pBlockViewRect);
                pBlockFrame.Dispose();

                if (outPanel != null)
                {
                    var outFrame = outPanel.DrawFrame();
                    DisplayInventory(ref outFrame, outViewRect);
                    outFrame.Dispose();
                }
            }

            private void DisplayInventory(ref MySpriteDrawFrame frame, RectangleF viewRect)
            {
                float xOffset = viewRect.Size.X / (outputColumns * 2);
                float cWidth = viewRect.Size.X / outputColumns;
                float yOffset = viewRect.Size.Y / (outputRows * 2);
                float rHeight = viewRect.Size.Y / outputRows;

                int entryIndex = 0;

                foreach (string name in inventoryManager.categories.Keys)
                {
                    // can probably change how this is accessed
                    Category category = inventoryManager.categories[name];

                    if (category.Inventories.Count > 0)
                    {
                        float x = viewRect.X + xOffset + (entryIndex / outputRows) * cWidth;
                        float y = viewRect.Y + yOffset + (entryIndex % outputRows) * rHeight;

                        Vector2 pos = new Vector2(x, y);

                        // getting this offset perfect probably depends on the size of the letters
                        pos.Y -= 20;

                        // apparenlty it's 1000000 (6 decimal places) to convert MyFixedPoint to kg
                        int amount = (int) Math.Round(category.CurrentAmount.RawValue / 1000000f);

                        string amountString = amount > 1000 ? $"{amount / 1000}k" : $"{amount}";

                        MySprite sprite = new MySprite()
                        {
                            Type = SpriteType.TEXT,
                            Data = amountString,
                            Position = pos,
                            RotationOrScale = 0.75f,
                            Color = Color.White,
                            Alignment = TextAlignment.CENTER,
                            FontId = "Monospace"
                        };
                        frame.Add(sprite);

                        pos.Y += 20;
                        sprite = new MySprite()
                        {
                            Type = SpriteType.TEXT,
                            Data = $"{name}",
                            Position = pos,
                            RotationOrScale = 0.75f,
                            Color = Color.White,
                            Alignment = TextAlignment.CENTER,
                            FontId = "Monospace"
                        };

                        frame.Add(sprite);
                        entryIndex++;
                    }
                }
            }
        }

        InventoryManager inventoryManager;
        Output output;

        public Program()
        {
            inventoryManager = new InventoryManager(this);
            output = new Output(this, inventoryManager);

            // 'register' for i/o updates
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {
            inventoryManager.LoadIni();
            inventoryManager.SaveIni();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // main i/o thread
            if ((updateSource & UpdateType.Update100) != 0)
            {
                // expensive probably, should do once every so many runs, idk
                inventoryManager.GetInventories();

                inventoryManager.SortItems();

                output.ShowOutput();

                // todo: optimize startup (~13ms is unacceptable on servers)
                Echo($"{Runtime.LastRunTimeMs}");
            }
        }
        #endregion
    }
}