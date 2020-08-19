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

        /*
         * there's still some unintended behaviour that should be addressed
         * not very important: items will not be sorted (even into the general category) if they are not in the dictionary
         */

        class Category {
            public string IniSection { get; }
            public string Type { get; }
            public string Tag { get; set; }
            public MyFixedPoint CurrentVolume { get; set; }
            public MyFixedPoint MaxVolume { get; set; }
            public List<IMyTerminalBlock> Inventories { get; } = new List<IMyTerminalBlock>();

            public Category(string iniSection, string type)
            {
                IniSection = iniSection;
                Type = type;
            }
        }

        class InventoryManager
        {
            Program parent;

            MyIni ini = new MyIni();

            public Dictionary<string, Category> categories { get; } = new Dictionary<string, Category>
            {
                // general item types
                ["Ore"] = new Category("general", "MyObjectBuilder_Ore"),
                ["Ingot"] = new Category("general", "MyObjectBuilder_Ingot"),
                ["Component"] = new Category("general", "MyObjectBuilder_Component"),
                ["Ammo"] = new Category("general", "MyObjectBuilder_AmmoMagazine"),
                ["Gun"] = new Category("general", "MyObjectBuilder_PhysicalGunObject"),
                ["Consumable"] = new Category("general", "MyObjectBuilder_ConsumableItem"),
                ["Gas Container"] = new Category("general", "MyObjectBuilder_GasContainerObject"),
                ["Oxygen Container"] = new Category("general", "MyObjectBuilder_OxygenContainerObject"),
                ["Datapad"] = new Category ("general", "MyObjectBuilder_Datapad"),
                ["Package"] = new Category("general", "MyObjectBuilder_Package"),
                ["Physical Object"] = new Category("general", "MyObjectBuilder_PhysicalObject"),

                // ores
                ["Stone"] = new Category("ores", "MyObjectBuilder_Ore/Stone"),
                ["Scrap Metal"] = new Category("ores", "MyObjectBuilder_Ore/Scrap"),
                ["Ice"] = new Category("ores", "MyObjectBuilder_Ore/Ice"),
                ["Iron Ore"] = new Category("ores", "MyObjectBuilder_Ore/Iron"),
                ["Nickel Ore"] = new Category("ores", "MyObjectBuilder_Ore/Nickel"),
                ["Silicon Ore"] = new Category("ores", "MyObjectBuilder_Ore/Silicon"),
                ["Cobalt Ore"] = new Category("ores", "MyObjectBuilder_Ore/Cobalt"),
                ["Magnesium Ore"] = new Category("ores", "MyObjectBuilder_Ore/Magnesium"),
                ["Silver Ore"] = new Category("ores", "MyObjectBuilder_Ore/Silver"),
                ["Gold Ore"] = new Category("ores", "MyObjectBuilder_Ore/Gold"),
                ["Uranium Ore"] = new Category("ores", "MyObjectBuilder_Ore/Uranium"),
                ["Platinum Ore"] = new Category("ores", "MyObjectBuilder_Ore/Platinum"),
                ["Organic"] = new Category("ores", "MyObjectBuilder_Ore/Organic"),

                // ingots
                ["Gravel"] = new Category("ingots", "MyObjectBuilder_Ingot/Stone"), // gravel = stone ingot, nice one keen
                ["Iron Ingot"] = new Category("ingots", "MyObjectBuilder_Ingot/Iron"),
                ["Nickel Ingot"] = new Category("ingots", "MyObjectBuilder_Ingot/Nickel"),
                ["Silicon Wafer"] = new Category("ingots", "MyObjectBuilder_Ingot/Silicon"),
                ["Cobalt Ingot"] = new Category("ingots", "MyObjectBuilder_Ingot/Cobalt"),
                ["Magnesium Powder"] = new Category("ingots", "MyObjectBuilder_Ingot/Magnesium"),
                ["Silver Ingot"] = new Category("ingots", "MyObjectBuilder_Ingot/Silver"),
                ["Gold Ingot"] = new Category("ingots", "MyObjectBuilder_Ingot/Gold"),
                ["Uranium Ingot"] = new Category("ingots", "MyObjectBuilder_Ingot/Uranium"),
                ["Platinum Ingot"] = new Category("ingots", "MyObjectBuilder_Ingot/Platinum"),
                ["Old Scrap Metal"] = new Category("ingots", "MyObjectBuilder_Ingot/Scrap"),

                // components
                ["Bulletproof Glass"] = new Category("components", "MyObjectBuilder_Component/BulletproofGlass"),
                ["Canvas"] = new Category("components", "MyObjectBuilder_Component/Canvas"),
                ["Computer"] = new Category("components", "MyObjectBuilder_Component/Computer"),
                ["Construction Comp."] = new Category("components", "MyObjectBuilder_Component/Construction"),
                ["Detector"] = new Category("components", "MyObjectBuilder_Component/Detector"),
                ["Display"] = new Category("components", "MyObjectBuilder_Component/Display"),
                ["Explosives"] = new Category("components", "MyObjectBuilder_Component/Explosives"),
                ["Girder"] = new Category("components", "MyObjectBuilder_Component/Girder"),
                ["Gravity Generator"] = new Category("components", "MyObjectBuilder_Component/GravityGenerator"),
                ["Interior Plate"] = new Category("components", "MyObjectBuilder_Component/InteriorPlate"),
                ["Large Tube"] = new Category("components", "MyObjectBuilder_Component/LargeTube"),
                ["Medical Comp."] = new Category("components", "MyObjectBuilder_Component/Medical"),
                ["Metal Grid"] = new Category("components", "MyObjectBuilder_Component/MetalGrid"),
                ["Motor"] = new Category("components", "MyObjectBuilder_Component/Motor"),
                ["Power Cell"] = new Category("components", "MyObjectBuilder_Component/PowerCell"),
                ["Radio-comm Comp."] = new Category("components", "MyObjectBuilder_Component/RadioCommunication"),
                ["Reactor Comp."] = new Category("components", "MyObjectBuilder_Component/Reactor"),
                ["Small Tube"] = new Category("components", "MyObjectBuilder_Component/SmallTube"),
                ["Solar Cell"] = new Category("components", "MyObjectBuilder_Component/SolarCell"),
                ["Steel Plate"] = new Category("components", "MyObjectBuilder_Component/SteelPlate"),
                ["Superconductor"] = new Category("components", "MyObjectBuilder_Component/Superconductor"),
                ["Thruster Comp."] = new Category("components", "MyObjectBuilder_Component/Thrust"),
                ["Zone Chip"] = new Category("components", "MyObjectBuilder_Component/ZoneChip"),

                // ammo
                ["200mm missile"] = new Category("ammo", "MyObjectBuilder_AmmoMagazine/Missile200mm"),
                ["25x184mm NATO"] = new Category("ammo", "MyObjectBuilder_AmmoMagazine/NATO_25x184mm"),
                ["5.56x45mm NATO"] = new Category("ammo", "MyObjectBuilder_AmmoMagazine/NATO_5p56x45mm"),

                // guns
                ["Welder"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/WelderItem"),
                ["Enhanced Welder"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/Welder2Item"),
                ["Proficient Welder"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/Welder3Item"),
                ["Elite Welder"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/Welder4Item"),

                ["Grinder"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/AngleGrinderItem"),
                ["Enhanced Grinder"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/AngleGrinder2Item"),
                ["Proficient Grinder"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/AngleGrinder3Item"),
                ["Elite Grinder"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/AngleGrinder4Item"),

                ["Hand Drill"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/HandDrillItem"),
                ["Enhanced Hand Drill"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/HandDrill2Item"),
                ["Proficient Hand Drill"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/HandDrill3Item"),
                ["Elite Hand Drill"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/HandDrill4Item"),

                ["Automatic Rifle"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/AutomaticRifleItem"),
                ["Elite Automatic Rifle"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/UltimateAutomaticRifleItem"),
                ["Precise Automatic Rifle"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/PreciseAutomaticRifleItem"),
                ["Rapid-Fire Automatic Rifle"] = new Category("guns", "MyObjectBuilder_PhysicalGunObject/RapidFireAutomaticRifleItem"),

                // consumables
                ["Clang Cola"] = new Category("consumables", "MyObjectBuilder_ConsumableItem/ClangCola"),
                ["Cosmic Coffee"] = new Category("consumables", "MyObjectBuilder_ConsumableItem/CosmicCoffee"),
                ["Medkit"] = new Category("consumables", "MyObjectBuilder_ConsumableItem/Medkit"),
                ["Powerkit"] = new Category("consumables", "MyObjectBuilder_ConsumableItem/Powerkit"),

                // gas containers
                ["Hydrogen Bottle"] = new Category("general", "MyObjectBuilder_GasContainerObject/HydrogenBottle"),

                // oxygen containers
                ["Oxygen Bottle"] = new Category("general", "MyObjectBuilder_OxygenContainerObject/OxygenBottle"),

                // packages
                ["Package Item"] = new Category("general", "MyObjectBuilder_Package/Package"), // doesn't really work with this system

                // datapad
                ["Datapad Item"] = new Category("datapad", "MyObjectBuilder_Datapad/Datapad"), // again

                // datapad
                ["Space Credit"] = new Category("physical object", "MyObjectBuilder_PhysicalObject/SpaceCredit") // again
            };

            int a = (int)Base6Directions.Direction.Up;

            List<IMyTerminalBlock> miscInventoryBlocks = new List<IMyTerminalBlock>();
            public MyFixedPoint CurrentMiscVolume { get; set; }
            public MyFixedPoint MaxMiscVolume { get; set; }

            // lookups
            Dictionary<string, string> typeToCategory = new Dictionary<string, string>();
            Dictionary<string, string> tagToCategory = new Dictionary<string, string>();

            // todo: make sure rebuilding this lookup every time isn't taking too long
            Dictionary<long, List<string>> idToCategories = new Dictionary<long, List<string>>();

            // holds all relevant blocks for each sorting run
            List<IMyTerminalBlock> inventoryBlocks = new List<IMyTerminalBlock>();

            // reusable list
            List<MyInventoryItem> items = new List<MyInventoryItem>();

            public InventoryManager(Program parent)
            {
                this.parent = parent;

                // get tags
                LoadIni();
                SaveIni();

                foreach (string name in categories.Keys)
                {
                    Category category = categories[name];
                    typeToCategory[category.Type] = name;
                    tagToCategory[category.Tag] = name;
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
                        categories[name].Tag = ini.Get(categories[name].IniSection, $"{iniName}_tag").ToString($"[{name.ToLower()}]");
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

                foreach (string name in categories.Keys)
                {
                    string iniName = name.ToLower().Replace(' ', '_');
                    ini.Set(categories[name].IniSection, $"{iniName}_tag", categories[name].Tag);
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
                    category.CurrentVolume = 0;
                    category.MaxVolume = 0;
                    category.Inventories.Clear();
                }

                miscInventoryBlocks.Clear();
                CurrentMiscVolume = MyFixedPoint.Zero;
                MaxMiscVolume = MyFixedPoint.Zero;

                foreach (List<string> blockCategories in idToCategories.Values)
                {
                    blockCategories.Clear(); // doing this instead of clearing the dictionary (that way we don't have to make new lists)
                }

                parent.GridTerminalSystem.GetBlocksOfType(inventoryBlocks, b => b.HasInventory);

                foreach (IMyTerminalBlock block in inventoryBlocks)
                {
                    if (block.HasInventory && !((block is IMyProductionBlock)
                        || (block is IMyPowerProducer) || (block is IMyGasGenerator)))
                    {
                        if (!idToCategories.ContainsKey(block.EntityId)) // to avoid creating new lists literally every run
                        {
                            idToCategories[block.EntityId] = new List<string>();
                        }

                        bool hasCategories = false;

                        foreach (string name in categories.Keys)
                        {
                            Category category = categories[name];

                            if (block.CustomName.Contains(category.Tag))
                            {
                                category.Inventories.Add(block);

                                IMyInventory inventory = block.GetInventory();
                                category.CurrentVolume += inventory.CurrentVolume;
                                category.MaxVolume += inventory.MaxVolume;

                                idToCategories[block.EntityId].Add(name);

                                hasCategories = true;
                            }
                        }
                        if (!hasCategories)
                        {
                            miscInventoryBlocks.Add(block);

                            IMyInventory inventory = block.GetInventory();
                            CurrentMiscVolume += inventory.CurrentVolume;
                            MaxMiscVolume += inventory.MaxVolume;
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

                        if (typeToCategory.ContainsKey(type) && typeToCategory.ContainsKey(subtype)) // for items that are known
                        {
                            string specificCategory = typeToCategory[subtype];
                            string generalCategory = typeToCategory[type];

                            bool moved = false;

                            if (categories[specificCategory].Inventories.Count > 0)
                            {
                                if (blockCategories.Contains(specificCategory))
                                {
                                    moved = true;
                                }
                                else
                                {
                                    if (MoveToTarget(block.GetInventory(), categories[specificCategory], item))
                                    {
                                        moved = true;
                                    }
                                }
                            }
                            if (!moved && categories[generalCategory].Inventories.Count > 0)
                            {
                                if (blockCategories.Contains(generalCategory))
                                {
                                    moved = true;
                                }
                                else
                                {
                                    if (MoveToTarget(block.GetInventory(), categories[generalCategory], item))
                                    {
                                        moved = true;
                                    }
                                }
                            }
                            if (!moved)
                            {
                                if (blockCategories.Count > 0)
                                {
                                    MoveToMisc(block.GetInventory(), item);
                                }
                            }
                        }
                        else // for unknown items
                        {
                            parent.Echo($"unknown item:\n{item.Type.ToString()}");
                            if (blockCategories.Count > 0) // this and similar above is to prevent pushing from misc. storage to misc. storage
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

                    if (source.CanTransferItemTo(destination, item.Type)) // not worrying about amounts
                    {
                        if (!destination.IsFull && source.TransferItemTo(destination, item)) // TransferItemTo returns true even if nothing was transferred
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            private void MoveToMisc(IMyInventory source, MyInventoryItem item)
            {
                /*
                 * try to move the item to an uncategorized inventory
                 */
                foreach (IMyTerminalBlock block in miscInventoryBlocks)
                {
                    IMyInventory destination = block.GetInventory();

                    if (source.CanTransferItemTo(destination, item.Type)) // not worrying about amounts
                    {
                        if (!destination.IsFull && source.TransferItemTo(destination, item)) // TransferItemTo returns true even if nothing was transferred
                        {
                            return;
                        }
                    }
                }
            }
        }

        class Panel
        {
            public IMyTextPanel TextPanel { get; }
            public RectangleF ViewRect { get; } = new RectangleF();

            public Panel(IMyTextPanel textPanel, RectangleF viewRect)
            {
                TextPanel = textPanel;
                ViewRect = viewRect;
            }
        }

        class Output
        {
            Program parent;
            InventoryManager inventoryManager;

            // terminal i/o
            Color foregroundColor = Color.Green;
            Color backgroundColor = Color.Black;

            IMyTextSurface pBlockSurface;
            RectangleF pBlockViewRect = new RectangleF();

            IMyTextPanel outPanel;
            RectangleF outViewRect = new RectangleF();

            // if you need more than 40 catergories to sort by, you might need help
            // that, or i have severely underestimated the users of this script
            // todo: try to make this expandable (preferably with multiple screens)
            int outputRows = 8;
            int outputColumns = 5;

            // reusable i/o
            Vector2 spritePosition = new Vector2();
            MySprite sprite; // not very beneficial since new sprites must be created anyways

            // reusable lists
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

            public Output(Program parent, InventoryManager inventoryManager)
            {
                this.parent = parent;
                this.inventoryManager = inventoryManager;

                GetDisplays();
            }

            public void GetDisplays()
            {
                pBlockSurface = parent.Me.GetSurface(0);
                pBlockSurface.ContentType = ContentType.SCRIPT;
                pBlockSurface.Script = string.Empty;
                pBlockSurface.ScriptForegroundColor = foregroundColor;
                pBlockSurface.ScriptBackgroundColor = backgroundColor;

                pBlockViewRect.Position = (pBlockSurface.TextureSize - pBlockSurface.SurfaceSize) / 2f;
                pBlockViewRect.Size = pBlockSurface.SurfaceSize;

                blocks.Clear();

                parent.GridTerminalSystem.SearchBlocksOfName("[out]", blocks, b => b is IMyTextPanel);

                if (blocks.Count > 0)
                {
                    outPanel = blocks[0] as IMyTextPanel;
                    outPanel.ContentType = ContentType.SCRIPT;
                    outPanel.Script = string.Empty;
                    outPanel.ScriptForegroundColor = foregroundColor;
                    outPanel.ScriptBackgroundColor = backgroundColor;

                    outViewRect.Position = (outPanel.TextureSize - outPanel.SurfaceSize) / 2f;
                    outViewRect.Size = outPanel.SurfaceSize;
                }
            }

            public void ShowOutput()
            {
                var pBlockFrame = pBlockSurface.DrawFrame();
                DisplayInventory(ref pBlockFrame, pBlockViewRect);
                pBlockFrame.Dispose();

                if (outPanel != null)
                {
                    MySpriteDrawFrame outFrame = outPanel.DrawFrame();
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

                double percent;
                int bars;

                int entryIndex = 0;

                foreach (string name in inventoryManager.categories.Keys) // category outputs
                {
                    Category category = inventoryManager.categories[name];

                    if (category.Inventories.Count > 0)
                    {
                        spritePosition.X = viewRect.X + xOffset + (entryIndex / outputRows) * cWidth;
                        spritePosition.Y = viewRect.Y + yOffset + (entryIndex % outputRows) * rHeight;

                        percent = (double) category.CurrentVolume.RawValue / category.MaxVolume.RawValue;
                        bars = (int) Math.Ceiling(percent * 9); // this will show one bar if an inventory has anything in it

                        AddEntry(ref frame, bars, name);

                        entryIndex++;
                    }
                }

                // misc. inventories
                spritePosition.X = viewRect.X + xOffset + (entryIndex / outputRows) * cWidth;
                spritePosition.Y = viewRect.Y + yOffset + (entryIndex % outputRows) * rHeight;

                percent = (double)inventoryManager.CurrentMiscVolume.RawValue / inventoryManager.MaxMiscVolume.RawValue;
                bars = (int) Math.Ceiling(percent * 9); // same as above

                AddEntry(ref frame, bars, "Other");
            }

            public void AddEntry(ref MySpriteDrawFrame frame, int bars, string name)
            {
                Color color = bars == 10 ? Color.Yellow : Color.White; // this does not spark joy

                string barString = $"[{new String('|', bars)}{new String('-', 10 - bars)}]";

                spritePosition.Y -= 12; // getting this offset perfect probably depends on the size of the letters

                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = barString,
                    Position = spritePosition,
                    RotationOrScale = 0.4f,
                    Color = color,
                    Alignment = TextAlignment.CENTER,
                    FontId = "Monospace"
                };
                frame.Add(sprite);

                spritePosition.Y += 12;
                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = $"{name}",
                    Position = spritePosition,
                    RotationOrScale = 0.4f,
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    FontId = "Monospace"
                };

                frame.Add(sprite);
            }
        }

        InventoryManager inventoryManager;
        Output output;

        int runCount = 0, runsPerAverage = 10;
        double totalTime = 0;

        public Program()
        {
            inventoryManager = new InventoryManager(this);
            output = new Output(this, inventoryManager);

            Runtime.UpdateFrequency = UpdateFrequency.Update100; // 'register' for i/o updates
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
                inventoryManager.GetInventories(); // expensive probably, should do once every so many runs, idk
                inventoryManager.SortItems();

                output.GetDisplays();
                output.ShowOutput();

                CheckPerformance();
            }
        }

        public void CheckPerformance()
        {
            totalTime += Runtime.LastRunTimeMs;
            runCount++;

            // todo: optimize startup (~5ms is good but maybe better)
            // todo: optimize (if possible) the regular execution (iterating through the giant dictionary isn't helping)
            Echo($"last run: {Runtime.LastRunTimeMs}ms");

            if (runCount % runsPerAverage == 0)
            {
                Echo($"average: {totalTime / runsPerAverage}ms\n(last {runsPerAverage} runs)");
                runCount = 0;
                totalTime = 0;
            }
        }
        #endregion
    }
}