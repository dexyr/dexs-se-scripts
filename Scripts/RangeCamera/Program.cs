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
        // apparently it's better to not allocate memory during runtime
        // so most variables are created here

        // lots of caching to do for optimization (for the block function calls)
        // yada yada something this that pre-mature optimization is bad

        // MyCommandLine commandLine = new MyCommandLine();

        MyIni ini = new MyIni();
        string cameraTag, cameraTagDefault = "dex_rangecam";
        double scanDistance, scanDistanceDefault = 50000;

        bool ready, targeting;

        int scanTimer;
        MyDetectedEntityInfo target;

        double oldDistance;

        IMyTextSurface pBlockSurface;
        RectangleF viewRect;

        List<IMyTerminalBlock> rCams = new List<IMyTerminalBlock>();

        public Program()
        {
            // for i/o updates
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            // set up ini
            ReadCustomData();
            WriteCustomData();

            // set up terminal i/o
            pBlockSurface = Me.GetSurface(0);
            pBlockSurface.ContentType = ContentType.SCRIPT;
            pBlockSurface.Script = "";
            // overloaded vector math, but that's okay
            viewRect = new RectangleF((pBlockSurface.TextureSize - pBlockSurface.SurfaceSize) / 2f, pBlockSurface.SurfaceSize);

            // grab cameras
            GridTerminalSystem.SearchBlocksOfName($"[{cameraTag}]", rCams, tBlock => tBlock is IMyCameraBlock);

            foreach (IMyTerminalBlock c in rCams)
            {
                IMyCameraBlock cBlock = c as IMyCameraBlock;
                cBlock.EnableRaycast = true;
                scanTimer = cBlock.TimeUntilScan(scanDistance);
            }

            // using custom name for 'hud' functionality
            Me.ShowOnHUD = true;
            ready = false;
            targeting = false;
        }

        public void Save()
        {
            ReadCustomData();
            WriteCustomData();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // scan on any triggered updates
            if ((updateSource & UpdateType.Trigger) != 0)
            {
                Scan();
            }
            // i/o updates
            if ((updateSource & UpdateType.Update100) != 0)
            {
                // terminal i/o
                var frame = pBlockSurface.DrawFrame();
                OutputToDisplay(ref frame);
                frame.Dispose();

                // hud i/o
                OutputToName();
            }
        }

        private void ReadCustomData()
        {
            MyIniParseResult result;
            if (ini.TryParse(Me.CustomData, out result))
            {
                cameraTag = ini.Get("general", "camera_tag").ToString(cameraTagDefault);
                scanDistance = ini.Get("general", "scan_distance").ToDouble(scanDistanceDefault);
            }
            else
            {
                cameraTag = cameraTagDefault;
                scanDistance = scanDistanceDefault;
            }
        }

        private void WriteCustomData()
        {
            ini.Set("general", "camera_tag", cameraTag);
            ini.SetComment("general", "camera_tag", "set a custom camera tag here");

            ini.Set("general", "scan_distance", scanDistance);
            ini.SetComment("general", "scan_distance", "set the scan distance here");

            Me.CustomData = ini.ToString();
        }

        private void Scan()
        {
            foreach (IMyTerminalBlock c in rCams)
            {
                IMyCameraBlock cBlock = (c as IMyCameraBlock);

                if (cBlock.CanScan(scanDistance))
                {
                    target = cBlock.Raycast(scanDistance);
                    scanTimer = cBlock.TimeUntilScan(scanDistance);

                    if (!target.IsEmpty())
                    {
                        targeting = true;
                        ready = false;
                        oldDistance = Vector3.Distance(Me.GetPosition(), target.HitPosition.Value);
                        Me.CustomName = $"{target.Name} ({scanTimer / 1000}s)";
                    }
                    else
                    {
                        targeting = false;
                        ready = false;
                        Me.CustomName = $"no target ({scanTimer / 1000}s)";
                    }
                }
                // can clear target here if needed
            }
        }

        private void OutputToDisplay(ref MySpriteDrawFrame frame)
        {
            Vector2 pos = viewRect.Position + viewRect.Size / 2f;

            MySprite sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = $"{rCams.Count} range cameras found",
                Position = pos,
                RotationOrScale = 0.75f,
                Color = Color.White,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            };

            frame.Add(sprite);
        }

        private void OutputToName()
        {
            if (!ready)
            {
                scanTimer -= (int)Math.Floor(Runtime.TimeSinceLastRun.TotalMilliseconds);
                if (scanTimer <= 0)
                    ready = true;
            }

            // i know there are more elegant ways to do this (dict + action) but w/e
            if (targeting)
            {
                // i'm 100% sure that changing the block name 3 times in one function is a sin
                Me.CustomName = $"{target.Name}";

                // i'm also aware that logic in an i/o function is bad form
                double distance = Vector3D.Distance(Me.GetPosition(), target.HitPosition.Value);

                if (oldDistance > distance)
                {
                    double eta = distance * Runtime.TimeSinceLastRun.TotalMilliseconds / (oldDistance - distance);
                    Me.CustomName = $" ETA: {(int)Math.Floor(eta / 1000)}s";
                }
                else
                {
                    Me.CustomName = " ETA: inf.";
                }

                oldDistance = distance;
            }
            else
            {
                Me.CustomName = "no target";
            }

            if (ready)
                Me.CustomName += " (ready)";
            else
                Me.CustomName += ($" ({scanTimer / 1000}s)");
        }
        #endregion
    }
}