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
        // forgive me for this
        // you may wonder why i'm using a class instead of a struct, it helps me sleep at night
        public class Category
        {
            public string Tag { get; set; }
            public string TypePrefix { get; }
            // what's the point of worrying about accessors with reference types anyway (no i'm serious this is a real question)
            public List<IMyInventory> Inventories { get; } 
            public MyFixedPoint CurrentVolume { get; set; }
            public MyFixedPoint MaxVolume { get; set; }

            public Category(string typePrefix)
            {
                TypePrefix = typePrefix;
                Inventories = new List<IMyInventory>();
            }
        }

        MyIni ini = new MyIni();

        // is this excessive? this feels excessive
        Dictionary<string, Category> categories = new Dictionary<string, Category>();
        Dictionary<string, string> prefixes = new Dictionary<string, string>();

        // resusable lists
        // i chose 'Blocks' instead of 'Inventories' because it's shorter
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        List<MyInventoryItem> items = new List<MyInventoryItem>();

        // terminal i/o
        List<IMyTerminalBlock> panels = new List<IMyTerminalBlock>();
        IMyTextSurface pBlockSurface;
        IMyTextPanel outPanel;
        RectangleF pBlockViewRect = new RectangleF();
        RectangleF outViewRect = new RectangleF();

        public Program()
        {
            // category setup
            // dual-linking because it makes life easier later
            categories.Add("ore", new Category("MyObjectBuilder_Ore"));
            prefixes.Add("MyObjectBuilder_Ore", "ore");
            categories.Add("ingot", new Category("MyObjectBuilder_Ingot"));
            prefixes.Add("MyObjectBuilder_Ingot", "ingot");
            categories.Add("component", new Category("MyObjectBuilder_Component"));
            prefixes.Add("MyObjectBuilder_Component", "component");
            categories.Add("ammo", new Category("MyObjectBuilder_AmmoMagazine"));
            prefixes.Add("MyObjectBuilder_AmmoMagazine", "ammo");
            
            // ideally this would have different routines to handle
            // ctrl+f for "misc" to see why (adding conditions everywhere for a single edge case is a sign of bad design)
            // but i like my silly Category class... it makes accessing data so easy
            categories.Add("misc", new Category("nothingwillmatchthis"));
            categories["misc"].Tag = string.Empty;

            // set up ini
            ReadCustomData();
            WriteCustomData();

            // for i/o updates
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            // set up terminal i/o
            pBlockSurface = Me.GetSurface(0);
            pBlockSurface.ContentType = ContentType.SCRIPT;
            pBlockSurface.Script = "";
            pBlockViewRect = new RectangleF((pBlockSurface.TextureSize - pBlockSurface.SurfaceSize) / 2f, pBlockSurface.SurfaceSize);

            GridTerminalSystem.SearchBlocksOfName("[out]", blocks, b => b is IMyTextPanel);
            if (blocks.Count > 0)
            {
                outPanel = (blocks[0] as IMyTextPanel);
                outPanel.ContentType = ContentType.SCRIPT;
                outPanel.Script = "";
                outViewRect = new RectangleF((outPanel.TextureSize - outPanel.SurfaceSize) / 2f, outPanel.SurfaceSize);
            }

            // find inventories
            // only takes lists of IMyTerminalBlock, which means annoying casting later
            // doing my own filtering here, may be a mistake
            GetInventories();
        }

        public void Save()
        {
            ReadCustomData();
            WriteCustomData();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // main i/o thread
            if ((updateSource & UpdateType.Update100) != 0)
            {
                // expensive probably, should do once every so many runs, idk
                GetInventories();

                SortItems();

                // terminal i/o
                var pBlockFrame = pBlockSurface.DrawFrame();
                OutputInventory(ref pBlockFrame, pBlockViewRect);
                pBlockFrame.Dispose();

                if (outPanel != null)
                {
                    var outFrame = outPanel.DrawFrame();
                    OutputInventory(ref outFrame, outViewRect);
                    outFrame.Dispose();
                }
            }
        }

        private void GetInventories()
        {
            Echo("Getting inventories");

            // don't forget to clear these like i did
            // seriously, i thought GetBlocks automatically cleared the lists
            // i found out the hard way that it doesn't
            blocks.Clear();
            GridTerminalSystem.GetBlocks(blocks);

            foreach (Category c in categories.Values)
            {
                c.Inventories.Clear();
                c.CurrentVolume = 0;
                c.MaxVolume = 0;
            }

            // no c++ joke
            for (int c = blocks.Count - 1; c >= 0; c--)
            {
                // creating a reference here shouldn't be an issue (i just wanna type less)
                IMyTerminalBlock block = blocks[c];
                if (block.HasInventory)
                {
                    if (block is IMyCargoContainer)
                    {
                        // there shouldn't be multiple inventories for cargo blocks
                        foreach (Category cat in categories.Values)
                        {
                            if (block.CustomName.Contains(cat.Tag))
                            {
                                // to save some typing
                                IMyInventory inventory = block.GetInventory();

                                cat.CurrentVolume += inventory.CurrentVolume;
                                cat.MaxVolume += inventory.MaxVolume;
                                cat.Inventories.Add(inventory);
                                blocks.RemoveAt(c);
                                break;
                            }
                        }
                    }
                    else // removes blocks that aren't cargo containers
                    {
                        blocks.RemoveAt(c);
                    }
                }
                else // removes blocks without inventories
                {
                    blocks.RemoveAt(c);
                }
            }
            // anything still in the list can be used for misc. storage
            for (int c = blocks.Count - 1; c >= 0; c--)
            {
                // again, saving my precious fingers
                Category misc = categories["misc"];
                IMyInventory inventory = blocks[c].GetInventory();

                misc.CurrentVolume += inventory.CurrentVolume;
                misc.MaxVolume += inventory.MaxVolume;
                misc.Inventories.Add(inventory);
                blocks.RemoveAt(c);
            }
        }

        // this should be refactored if the script is too slow
        private void SortItems()
        {
            Echo("sorting");
            // push items out of the wrong inventories
            // i'm using the key because it's necessary for PushItem
            foreach (string catName in categories.Keys)
            {
                foreach (IMyInventory inv in categories[catName].Inventories)
                {
                    items.Clear();
                    inv.GetItems(items);

                    foreach (MyInventoryItem i in items)
                    {
                        // strings have never been elegant
                        // not sure if this would conflict with mods (that's something i don't wanna bother with)
                        string typePrefix = i.Type.ToString().Split('/')[0];
                        Echo($"item type: {i.Type.ToString()}");
                        Echo($"category type: {categories[catName].TypePrefix}");

                        // item is in the wrong inventory
                        if (!typePrefix.Equals(categories[catName].TypePrefix))
                            PushItem(catName, inv, i, typePrefix);
                    }
                }
            }
        }

        // this is here so i don't have >6 nested statements in one function
        // look at all those parameters
        private void PushItem(string sourceCategory, IMyInventory source, MyInventoryItem item, string typePrefix)
        {
            Echo("Pushing items");
            string destName;
            prefixes.TryGetValue(typePrefix, out destName);

            // ahhhhh, null checks, my favorite
            if (destName != null)
            {
                // i bet i can do this without a flag
                bool success = false;

                // first try to transfer to the correct inventory
                foreach (IMyInventory destination in categories[destName].Inventories)
                {
                    Echo($"{item} from {sourceCategory} to {destName}?");
                    if (source.CanTransferItemTo(destination, item.Type))
                        if (source.TransferItemTo(destination, item)) // transfers as much as possible
                        {
                            success = true;
                            break;
                        }
                }

                // otherwise just send it to a misc. inventory
                if (!success)
                {
                    // no point in moving from misc to misc
                    if (!sourceCategory.Equals("misc"))
                    {
                        // who needs braces
                        foreach (IMyInventory destination in categories["misc"].Inventories)
                            if (source.CanTransferItemTo(destination, item.Type))
                                if (source.TransferItemTo(destination, item))
                                    break;
                    }
                }
            }
            else // unsortable item (no matching category)
            {
                if (!sourceCategory.Equals("misc"))
                {
                    foreach (IMyInventory destination in categories["misc"].Inventories)
                        if (source.CanTransferItemTo(destination, item.Type))
                            if (source.TransferItemTo(destination, item))
                                break;
                }
            }
        }
        
        // reusing this as well
        // should add support for larger terminals
        private void OutputInventory(ref MySpriteDrawFrame frame, RectangleF viewRect)
        {
            // max 10-ish? for 2 cols of 5
            int barIndex = 0;
            float xOffset = viewRect.Size.X / (2f * 2);
            float cWidth = viewRect.Size.X / 2f;
            float yOffset = viewRect.Size.Y / (4f * 2);
            float rHeight = viewRect.Size.Y / (4f);

            foreach (string name in categories.Keys)
            {

                Category cat = categories[name];

                if (cat.Inventories.Count > 0)
                {
                    float x = viewRect.X + xOffset + (barIndex / 4) * cWidth;
                    float y = viewRect.Y + yOffset + (barIndex % 4) * rHeight;

                    Vector2 pos = new Vector2(x, y);


                    pos.Y -= 10;
                    double percent = cat.CurrentVolume.RawValue / cat.MaxVolume.RawValue;
                    int filled = (int)Math.Round(percent * 10);
                    string bar = $"[{new String('|', filled)}{new String('-', 10 - filled)}]";

                    MySprite sprite = new MySprite()
                    {
                        Type = SpriteType.TEXT,
                        Data = $"{bar}",
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
                    barIndex++;
                }
            }
        }


        // reusing this sort of thing
        // i'm aware that saving ini in custom data isn't recommended, but it's only read/written on recompile and saves
        private void ReadCustomData()
        {
            // not using the result since i'm not printing error messages (just gotta believe)
            // MyIniParseResult result;
            if (ini.TryParse(Me.CustomData))
            {
                foreach (string name in categories.Keys)
                {
                    if (!name.Equals("misc"))
                        categories[name].Tag = ini.Get("general", $"{name}_tag").ToString($"[{name}]");
                }
            }
            else // invalid/missing ini
            {
                foreach (string name in categories.Keys)
                {
                    if (!name.Equals("misc"))
                        categories[name].Tag = $"[{name}]";
                }
            }
        }

        private void WriteCustomData()
        {
            foreach (string name in categories.Keys)
            {
                if (!name.Equals("misc"))
                {
                    ini.Set("general", $"{name}_tag", categories[name].Tag);
                    ini.SetComment("general", $"{name}_tag", $"set the {name} tag here");
                }
            }

            // i wish i could put this above the loop (since that's where it shows in-game)
            // but MyIni will get very unhappy if the section doesn't exist
            // and apparently adding a section without a key-value pair just isn't a thing
            ini.SetSectionComment("general", "only works on cargo containers\ntag must be added to block name\n" +
                "untagged cargo containers will be used for misc. storage\nrecompile if you change anything");

            Me.CustomData = ini.ToString();
        }
        #endregion
    }
}