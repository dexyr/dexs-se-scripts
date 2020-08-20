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
            /*
             * the main thing that makes this work
             * this handles the different sorting categories that items/blocks can belong to
             */

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
            /*
             * it's in the name; handles 'sorting'
             */

            Program parent;

            MyIni ini = new MyIni();

            public Dictionary<string, Category> Categories { get; } = new Dictionary<string, Category>
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

                // space credit
                ["Space Credit"] = new Category("physical object", "MyObjectBuilder_PhysicalObject/SpaceCredit") // again
            };

            public Dictionary<string, Category> ActiveCategories { get; } = new Dictionary<string, Category>(); // hoping this helps performance

            List<IMyTerminalBlock> miscInventoryBlocks = new List<IMyTerminalBlock>();
            public MyFixedPoint CurrentMiscVolume { get; set; }
            public MyFixedPoint MaxMiscVolume { get; set; }

            // lookups
            Dictionary<string, string> typeToCategoryKey = new Dictionary<string, string>();
            Dictionary<string, string> tagToCategoryKey = new Dictionary<string, string>();

            // todo: make sure rebuilding this lookup every time isn't taking too long
            // this might be overkill, since most inventories will only have a small number of assigned ategories
            Dictionary<long, HashSet<string>> idToCategoryKeySet = new Dictionary<long, HashSet<string>>();

            // valid blocks for the sorting system
            List<IMyTerminalBlock> inventoryBlocks = new List<IMyTerminalBlock>();

            // reusable list
            List<MyInventoryItem> items = new List<MyInventoryItem>();

            // reusable newline thingy cause why, why doesn't c# have a built-in line-split function
            char[] newLine = { '\n' };

            public InventoryManager(Program parent)
            {
                this.parent = parent;

                // get tags
                LoadIni();
                SaveIni();

                foreach (string name in Categories.Keys)
                {
                    Category category = Categories[name];
                    typeToCategoryKey[category.Type] = name; // this could be declared above
                    tagToCategoryKey[category.Tag] = name; // this cannot (needs ini to be loaded)
                }
            }

            public void LoadIni()
            {
                /*
                 * loading tags from custom data
                 */

                if (ini.TryParse(parent.Me.CustomData))
                {
                    foreach (string name in Categories.Keys)
                    {
                        string iniName = name.ToLower().Replace(' ', '_');
                        Categories[name].Tag = ini.Get(Categories[name].IniSection, $"{iniName}_tag").ToString($"{name.ToLower()}");
                    }
                }
                else // invalid/missing ini
                {
                    foreach (string name in Categories.Keys)
                        Categories[name].Tag = $"{name.ToLower()}";
                }
            }

            public void SaveIni()
            {
                /*
                 * saving tags to custom data
                 */

                foreach (string name in Categories.Keys)
                {
                    string iniName = name.ToLower().Replace(' ', '_');
                    ini.Set(Categories[name].IniSection, $"{iniName}_tag", Categories[name].Tag);
                }

                // i wish i could put this above the loop (since that's where it shows in-game)
                // but MyIni will get very unhappy if the section doesn't exist
                // and apparently adding a section without a key-value pair just isn't a thing
                ini.SetSectionComment("general", "tag must be included in block name\n" +
                        "untagged inventories will be used for misc. storage\n" +
                        "recompile to update tags");

                parent.Me.CustomData = ini.ToString();
            }

            public void UpdateInventories()
            {
                /*
                 * grabs all blocks with inventories on the grid (except assemblers, reactors, etc.)
                 * adds blocks to different sorting categories (potentially multiple)
                 */

                ClearGridInfo();

                inventoryBlocks.Clear();
                parent.GridTerminalSystem.GetBlocksOfType(inventoryBlocks, b => InventoryBlockIsValid(b));

                foreach (IMyTerminalBlock block in inventoryBlocks)
                {
                    if (!idToCategoryKeySet.ContainsKey(block.EntityId)) // to avoid creating new dictionaries literally every run
                        idToCategoryKeySet[block.EntityId] = new HashSet<string>();

                    if (!AddCategoriesToBlock(block))
                    {
                        miscInventoryBlocks.Add(block);

                        IMyInventory inventory = block.GetInventory();
                        CurrentMiscVolume += inventory.CurrentVolume;
                        MaxMiscVolume += inventory.MaxVolume;
                    }
                }
            }

            private bool InventoryBlockIsValid(IMyTerminalBlock block)
            {
                /*
                 * used in the lambda above
                 */

                return block.HasInventory && !(block is IMyProductionBlock ||
                    block is IMyPowerProducer || block is IMyGasGenerator);
            }

            private void ClearGridInfo()
            {
                /*
                 * there's probably a better name, but this clears the knowledge of grid blocks so they can be repopulated
                 */

                // todo: fix a lot of this. Clear() is probably making the gc very upset
                // prioritizing gc over complexity may be the best thing to do
                // that or only modifying based on changes
                
                inventoryBlocks.Clear();

                foreach (Category category in ActiveCategories.Values)
                {
                    category.CurrentVolume = 0;
                    category.MaxVolume = 0;
                    category.Inventories.Clear();
                }
                ActiveCategories.Clear();

                miscInventoryBlocks.Clear();
                CurrentMiscVolume = MyFixedPoint.Zero;
                MaxMiscVolume = MyFixedPoint.Zero;

                // this could eventually get bloated if many blocks are removed from the grid since they will remain in the dictionary
                // or if the grid changes (and all blockids are changed)
                foreach (HashSet<string> categoryKeySet in idToCategoryKeySet.Values)
                    categoryKeySet.Clear(); // doing this instead of clearing the dictionary (that way we don't have to make new hashsets)
            }

            private bool AddCategoriesToBlock(IMyTerminalBlock block)
            {
                /*
                 * try (and add) all relevant categories to a block, mark the category as active, and increment inventory counts
                 * true if any category was added, false if not
                 */

                bool hasCategory = false;

                string[] customData = block.CustomData.Split(newLine);

                foreach (string tag in customData)
                {
                    string categoryKey;

                    if (tagToCategoryKey.TryGetValue(tag, out categoryKey))
                    {
                        Category category = Categories[categoryKey];

                        ActiveCategories[categoryKey] = category; // doesn't really matter what's used for this key

                        category.Inventories.Add(block);

                        IMyInventory inventory = block.GetInventory();
                        category.CurrentVolume += inventory.CurrentVolume;
                        category.MaxVolume += inventory.MaxVolume;

                        idToCategoryKeySet[block.EntityId].Add(categoryKey);

                        hasCategory = true;
                    }
                }
                return hasCategory;
            }
            
            public void SortItems()
            {
                /*
                 * push items towards the 'best' inventory
                 */

                foreach (IMyTerminalBlock block in inventoryBlocks) // if inventoryBlocks isn't up to date (possible), this will crash and burn
                {
                    items.Clear();
                    block.GetInventory().GetItems(items);

                    foreach (MyInventoryItem item in items)
                        PushItem(block, item);
                }   
            }
            
            private void PushItem(IMyTerminalBlock from, MyInventoryItem item)
            {
                /*
                 * tries to push item to the most appropriate inventory, or returns if it can't
                 * there's some repeated code here, but come on, i'm not writing a function for 2 calls
                 */

                HashSet<string> blockCategoryKeySet = idToCategoryKeySet[from.EntityId];

                // it might be more accurate to call this 'full type'
                // i'm not using the actual subtype because it would cause conflicts between ingots/ores
                string subtype = item.Type.ToString();
                string type = item.Type.TypeId;

                string categoryKey;

                if (typeToCategoryKey.TryGetValue(subtype, out categoryKey)) // to avoid exceptions with unregistered items
                {
                    if (blockCategoryKeySet.Contains(categoryKey)) // already in the right place
                        return;
                    if (Categories[categoryKey].Inventories.Count > 0) // if there's a place to put it
                    {
                        if (TryMoveToTarget(from.GetInventory(), Categories[categoryKey], item)) // if it could actually be moved to any matching chest
                            return;
                    }
                }
                if (typeToCategoryKey.TryGetValue(type, out categoryKey))
                {
                    if (blockCategoryKeySet.Contains(categoryKey)) // already in the right place
                        return;
                    if (Categories[categoryKey].Inventories.Count > 0) // if there's a place to put it
                    {
                        if (TryMoveToTarget(from.GetInventory(), Categories[categoryKey], item)) // if it could actually be moved to any matching chest
                            return;
                    }
                }
                if (blockCategoryKeySet.Count > 0) // if the current inventory isn't misc.
                    MoveToMisc(from.GetInventory(), item);
            }

            private bool TryMoveToTarget(IMyInventory source, Category target, MyInventoryItem item)
            {
                /*
                 * try to move the item from the source inventory to an inventory of the appropriate category
                 */

                foreach (IMyTerminalBlock block in target.Inventories)
                {
                    IMyInventory destination = block.GetInventory();

                    // TransferItemTo() returns true even if nothing was moved (but the type was appropriate)
                    if (source.CanTransferItemTo(destination, item.Type) && !destination.IsFull) // connected and room available
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

                foreach (IMyTerminalBlock block in miscInventoryBlocks)
                {
                    IMyInventory destination = block.GetInventory();

                    if (source.CanTransferItemTo(destination, item.Type) && !destination.IsFull) // same as above
                    {
                        if (source.TransferItemTo(destination, item)) // TransferItemTo returns true even if nothing was transferred
                            return;
                    }
                }
            }
        }

        class Surface
        {
            /*
             * just for holding data together, could be used for expanding the displays
             * would be more useful with multiple outputs
             */

            Color foregroundColor = Color.LightGreen;
            Color backgroundColor = Color.Black;

            public IMyTextSurface TextSurface { get; }
            public RectangleF ViewRect { get; } = new RectangleF();

            public Surface(IMyTextSurface textSurface, RectangleF viewRect)
            {
                TextSurface = textSurface;
                ViewRect = viewRect;

                ConfigurePanel();
            }

            private void ConfigurePanel()
            {
                TextSurface.ContentType = ContentType.SCRIPT;
                TextSurface.Script = string.Empty;
                TextSurface.ScriptForegroundColor = foregroundColor;
                TextSurface.ScriptBackgroundColor = backgroundColor;
            }
        }

        class Output
        {
            /*
             * handles, well, output
             */

            Program parent;
            InventoryManager inventoryManager;

            Surface pBlockSurface;

            // if you need more than 40 catergories to sort by, you might need help
            // that, or i have severely underestimated the users of this script
            // xtodo: try to make this expandable (preferably with multiple screens)
            // todo: the goals of this script have changed, counting inventory is not a priority right now
            int outputRows = 8;
            int outputColumns = 5;

            // reusable i/o
            Vector2 spritePosition = new Vector2();
            MySprite sprite; // not very beneficial since new sprites must be created anyways

            // reusable lists
            List<IMyTerminalBlock> panelBlocks = new List<IMyTerminalBlock>();

            public Output(Program parent, InventoryManager inventoryManager)
            {
                this.parent = parent;
                this.inventoryManager = inventoryManager;

                UpdateDisplays();
            }
            
            public void UpdateDisplays()
            {
                /*
                * refresh the display panels
                */

                IMyTextSurface surface = parent.Me.GetSurface(0);
                RectangleF viewRect = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);

                pBlockSurface = new Surface(surface, viewRect);
            }

            public void ShowOutput()
            {
                /*
                 * placeholder incase there are multiple output functions for different types of panels
                 */

                DisplayPerformance(pBlockSurface);
            }

            private void DisplayPerformance(Surface surface)
            {
                /*
                 * display the last run times on the surface
                 */

                MySpriteDrawFrame frame = surface.TextSurface.DrawFrame();
                RectangleF viewRect = surface.ViewRect;
                Color color = surface.TextSurface.ScriptForegroundColor;

                spritePosition = viewRect.Center;
                spritePosition.Y -= 20;

                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = $"last run: {parent.LastRunTime}ms ({parent.LastRunType})" +
                           $"\nlast average: {parent.LastAverage}ms ({parent.RunsPerAverage} runs)",
                    Position = spritePosition,
                    RotationOrScale = 0.6f,
                    Color = color,
                    Alignment = TextAlignment.CENTER,
                    FontId = "Monospace"
                };
                frame.Add(sprite);
                frame.Dispose();
            }

            private void DisplayInventory(Surface surface)
            {
                /*
                 * currently unused
                 * displays inventory information on the surface
                 */

                MySpriteDrawFrame frame = surface.TextSurface.DrawFrame();
                RectangleF viewRect = surface.ViewRect;

                float xOffset = viewRect.Size.X / (outputColumns * 2);
                float cWidth = viewRect.Size.X / outputColumns;
                float yOffset = viewRect.Size.Y / (outputRows * 2);
                float rHeight = viewRect.Size.Y / outputRows;

                double percent;
                int bars;

                int entryIndex = 0;

                foreach (string name in inventoryManager.ActiveCategories.Keys) // category outputs
                {
                    Category category = inventoryManager.ActiveCategories[name];

                    spritePosition.X = viewRect.X + xOffset + (entryIndex / outputRows) * cWidth;
                    spritePosition.Y = viewRect.Y + yOffset + (entryIndex % outputRows) * rHeight;

                    percent = (double) category.CurrentVolume.RawValue / category.MaxVolume.RawValue;
                    bars = (int) Math.Ceiling(percent * 9); // todo: fix this

                    AddEntry(ref frame, bars, name);

                    entryIndex++;
                }

                // misc. inventories
                spritePosition.X = viewRect.X + xOffset + (entryIndex / outputRows) * cWidth;
                spritePosition.Y = viewRect.Y + yOffset + (entryIndex % outputRows) * rHeight;

                percent = (double)inventoryManager.CurrentMiscVolume.RawValue / inventoryManager.MaxMiscVolume.RawValue;
                bars = (int) Math.Ceiling(percent * 9); // todo: and this

                AddEntry(ref frame, bars, "Other");

                frame.Dispose();
            }

            private void AddEntry(ref MySpriteDrawFrame frame, int bars, string name)
            {
                /*
                 * currently unused
                 * adds entry for category info
                 */

                Color color = bars == 10 ? Color.Yellow : Color.White; // this does not spark joy

                string barString = $"[{new String('|', bars)}{new String('-', 10 - bars)}]";

                spritePosition.Y -= 12; // getting this offset perfect probably depends on the size of the letters

                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = $"{barString}",
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

        // ty https://github.com/malware-dev/MDK-SE/wiki/Coroutines---Run-operations-over-multiple-ticks
        IEnumerator<bool> routine;

        InventoryManager inventoryManager;
        Output output;

        // performance metrics
        public int RunCount { get; set; } = 0;

        public int RunsPerAverage { get; } = 10;
        public double TotalTimeSinceLastAverage { get; set; } = 0;

        public double LastRunTime { get; set; }
        public double LastAverage { get; set; }
        public string LastRunType { get; set; }

        public Program()
        {
            inventoryManager = new InventoryManager(this);
            output = new Output(this, inventoryManager);

            Runtime.UpdateFrequency = UpdateFrequency.Update100; // 'register' for i/o updates

            routine = DoRoutine();
        }

        public void Save()
        {
            inventoryManager.LoadIni();
            inventoryManager.SaveIni();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((updateSource & UpdateType.Update100) != 0) // main 'thread'
            {
                if (!routine.MoveNext())
                {
                    routine.Dispose();
                    routine = DoRoutine();
                }

                output.ShowOutput();

                UpdatePerformance();
            }
        }

        private void UpdatePerformance()
        {
            /*
             * update performance stats
             */

            LastRunTime = Runtime.LastRunTimeMs;
            TotalTimeSinceLastAverage += LastRunTime;
            RunCount++;

            if (RunCount % RunsPerAverage == 0)
            {
                LastAverage = TotalTimeSinceLastAverage / RunsPerAverage;
                RunCount = 0;
                TotalTimeSinceLastAverage = 0;
            }
        }

        private IEnumerator<bool> DoRoutine()
        {
            /*
             * the two most expensive operations will run on separate calls to improve game performance
             */

            LastRunType = "sorting";
            inventoryManager.UpdateInventories();
            output.UpdateDisplays();

            yield return true;

            LastRunType = "updating blocks";
            inventoryManager.SortItems();
        }
        #endregion
    }
}
