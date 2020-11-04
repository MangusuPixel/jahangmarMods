﻿// Copyright (c) 2020 Jahangmar
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using StardewValley;
using Microsoft.Xna.Framework;
using xTile.Layers;
using xTile.Tiles;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework.Audio;

namespace AccessibilityForBlind
{
    enum BarrierType
    {
        Unknown, UnknownObject, UnknownTerrain, UnknownLargeTerrain, Wood, LargeWood, Stone, LargeStone, Weed, Crop, Bush, Tree, ActionObject, UnknownWall, WoodWall, StoneWall, Water,
        Fence, Lantern, Housewall, Bench, Cliff, PetWaterBowl
    }


    public static class GameplaySounds
    {
        private static Vector2 oldPlayerPos;
        private static int FacingDir = 0;

        public static void Init()
        {
            ModEntry.GetHelper().Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            ModEntry.GetHelper().Events.Input.ButtonPressed += Input_ButtonPressed;
            ModEntry.GetHelper().Events.Input.ButtonReleased += Input_ButtonReleased;
            ModEntry.GetHelper().Events.GameLoop.OneSecondUpdateTicked += GameLoop_OneSecondUpdateTicked;
            ModEntry.GetHelper().Events.Player.Warped += Player_Warped;
            //Microsoft.Xna.Framework.Audio.SoundBank
            //  ->playCue has 3D option
            //Game1.playSoundPitched might be blueprint for custom sound function
        }

        static void Player_Warped(object sender, StardewModdingAPI.Events.WarpedEventArgs e)
        {
            if (!e.OldLocation.IsOutdoors && e.NewLocation.IsOutdoors || e.OldLocation is FarmHouse && e.NewLocation.isFarm)
            {
                Game1.playSound("doorOpen"); //"doorClose" "doorCreakReverse"
            }
            else// if (e.OldLocation.IsOutdoors && e.NewLocation.IsOutdoors)
            {
                Game1.playSound("dwop");
            }

            if (e.NewLocation.Name.Length > 0)
            {
                TextToSpeech.Speak("entering "+e.NewLocation.Name);
            }
        }

        static void Input_ButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (!StardewModdingAPI.Context.IsWorldReady)
                return;
            //xTile.Dimensions.Location location = new xTile.Dimensions.Location((int)Game1.player.Position.X + Game1.tileSize / 2, (int)Game1.player.Position.Y + Game1.tileSize / 2);
            //ShowTileInfo(location);

            SetFacingDir();

            if (Game1.activeClickableMenu == null)
            {
                switch (e.Button)
                {
                    case StardewModdingAPI.SButton.Enter:
                        //ModEntry.Log($"dir: {Game1.player.getDirection()}, fdir: {Game1.player.getFacingDirection()}");
                        string descr = FindBarrierDescription();
                        if (descr.Length > 0)
                            TextToSpeech.Speak("You feel a " + descr);
                        break;
                }
            }
        }

        static void Input_ButtonReleased(object sender, StardewModdingAPI.Events.ButtonReleasedEventArgs e)
        {
            if (!StardewModdingAPI.Context.IsWorldReady)
                return;

            if ((int)Game1.options.inventorySlot1[0].key == (int)e.Button
                || (int)Game1.options.inventorySlot2[0].key == (int)e.Button
                || (int)Game1.options.inventorySlot3[0].key == (int)e.Button
                || (int)Game1.options.inventorySlot4[0].key == (int)e.Button
                || (int)Game1.options.inventorySlot5[0].key == (int)e.Button
                || (int)Game1.options.inventorySlot6[0].key == (int)e.Button
                || (int)Game1.options.inventorySlot7[0].key == (int)e.Button
                || (int)Game1.options.inventorySlot8[0].key == (int)e.Button
                || (int)Game1.options.inventorySlot9[0].key == (int)e.Button
                || (int)Game1.options.inventorySlot10[0].key == (int)e.Button
                || (int)Game1.options.inventorySlot11[0].key == (int)e.Button
                || (int)Game1.options.inventorySlot12[0].key == (int)e.Button)
            {
                if (Game1.player.CurrentItem != null)
                {
                    TextToSpeech.Speak(TextToSpeech.ItemToSpeech(Game1.player.CurrentItem));
                }
            }
        }


        static bool blocked = false;
        static int blockTimer = 0;

        static void GameLoop_UpdateTicked(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (!StardewModdingAPI.Context.CanPlayerMove)
                return;

            bool oldblocked = blocked;

            blocked = (Game1.currentLocation != null && Game1.player.isMoving() && Game1.player.Position.Equals(oldPlayerPos));

            SetFacingDir();

            if (blocked)
            {
                if (!oldblocked || Game1.player.getDirection() != FacingDir)//reset if wasn't blocked before or direction changed
                    blockTimer = 0;
                else
                    blockTimer += 1;

                if (blockTimer % 60 == 0)
                    PlaySoundForBarrier(FindBarrierType());
            }

            oldPlayerPos = Game1.player.Position;
        }

        static void SetFacingDir()
        {
            int dir = Game1.player.getDirection();
            if (dir != -1)
                FacingDir = dir;
        }

        static void GameLoop_OneSecondUpdateTicked(object sender, StardewModdingAPI.Events.OneSecondUpdateTickedEventArgs e)
        {
            if (!StardewModdingAPI.Context.IsWorldReady)
                return;
            //TODO make noise for interactable objects/people/warps/...
            foreach (Character character in Game1.currentLocation.getCharacters())
            {
                Vector2 charPos = character.Position;
                if (Utility.distance(charPos.X, Game1.player.Position.X, charPos.Y, Game1.player.Position.Y) < 10*Game1.tileSize)
                {
                    PlaySound("stoneStep", charPos / Game1.tileSize); //TODO step depends on tile
                }
            }
        }

        private static void PlaySound(string cueName, Vector2 sourceTilePos)
        {
            try
            {
                SoundBank soundBank =  ModEntry.GetHelper().Reflection.GetField<SoundBank>(Game1.soundBank as SoundBankWrapper, "soundBank").GetValue();
                Cue cue = soundBank.GetCue(cueName);
                AudioListener listener = new AudioListener
                {
                    Position = new Vector3(Game1.player.Position, 0)
                };
                AudioEmitter emitter = new AudioEmitter
                {
                    Position = new Vector3(sourceTilePos*Game1.tileSize, 0)
                };

                cue.Apply3D(listener, emitter);
                cue.Play();
            }
            catch (System.Exception e)
            {
                ModEntry.Log("Error while playing sound: " + e.Message);
            }
        }

        private static BarrierType FindBarrierType()
        {
            Vector2 nextPos = PlayerNextPosition();

            xTile.Dimensions.Location location = new xTile.Dimensions.Location((int)nextPos.X, (int)nextPos.Y);
            Vector2 tilePos = new Vector2(location.X / Game1.tileSize, location.Y / Game1.tileSize);
            GameLocation loc = Game1.currentLocation;

            Layer buildingsLayer = loc.map.GetLayer("Buildings");
            Tile tile = buildingsLayer.PickTile(location, Game1.viewport.Size);

            if (tile != null)
            {
                int i = tile.TileIndex;
                ModEntry.Log("tile "+tile.TileIndex);
                if (loc is FarmHouse)
                    return BarrierType.Housewall;
                else if (i == 184 || i == 1183 || i == 1210 || i == 211 || i == 183 || i == 1182 || i == 185 || i == 1184 || i == 235 || i == 1290 || i == 260 || i == 1315
                || i == 259 || i == 258 || i == 1257 || i == 1207 || i == 208 || i == 213 || i == 212 || i == 1232 || i == 233 || i == 1209 || i == 210 || i == 238 || i == 1292
                    || i == 237 || i == 1291)
                    return BarrierType.Water;
                else if (i == 383 || i == 384 || i == 385 || i == 436 || i == 411 || i == 386 || i == 434
                 || i == 832 || i == 833 || i == 834 || i == 866 || i == 898 || i == 930 || i == 900 || i == 929 || i == 928 || i == 896 || i == 864)
                    return BarrierType.Fence;
                else if (i == 541 || i == 542 || i == 543 || i == 544 || i == 391 || i == 416 || i == 441 || i == 466 || i == 491 || i == 516 || i == 419 || i == 394 || i == 444
                || i == 469 || i == 494 || i == 519 || i == 422 || i == 438 || i == 540 || i == 439 || i == 464 || i == 440 || i == 467 || i == 468 || i == 369
                || i == 344 || i == 319 || i == 294 || i == 295 || i == 291 || i == 316 || i == 366 || i == 496 || i == 522 || i == 548 || i == 547 || i == 546
                || i == 545 || i == 539 || i == 371 || i == 399 || i == 446 || i == 290)
                    return BarrierType.Cliff;
                else if (i == 1938 || i == 1939)
                    return BarrierType.PetWaterBowl;
                else if (i == 40 || i == 72 || i == 322 || i == 323 || i == 327 || i == 54 || i == 86)
                    return BarrierType.Bench;
                else if (i == 1003)
                    return BarrierType.Lantern;
                return BarrierType.UnknownWall;
            }
            else if (loc.isTerrainFeatureAt((int)tilePos.X, (int)tilePos.Y))
            {
                TerrainFeature terrainFeature = loc.terrainFeatures.ContainsKey(tilePos) ? loc.terrainFeatures[tilePos] : loc.getLargeTerrainFeatureAt((int)tilePos.X, (int)tilePos.Y);
                ModEntry.Log("terrain: "+terrainFeature.ToString());
                if (terrainFeature is Bush)
                    return BarrierType.Bush;
                if (terrainFeature is Tree)
                    return BarrierType.Tree;
                return BarrierType.UnknownTerrain;
            }
            /*
            else if (Game1.currentLocation.getLargeTerrainFeatureAt(location.X / Game1.tileSize, location.Y / Game1.tileSize) != null)
            {
                ModEntry.Log("large terrain");
                return BarrierType.UnknownLargeTerrain;
            }
            */
            else if (Game1.currentLocation.getObjectAtTile(location.X / Game1.tileSize, location.Y / Game1.tileSize) is StardewValley.Object obj && obj != null)
            {
                switch (obj.ParentSheetIndex)
                {
                    case 294:
                    case 295:
                        return BarrierType.Wood;
                    case Object.mineStoneSnow1:
                    case Object.mineStoneSnow2:
                    case Object.mineStoneSnow3:
                    case Object.mineStonePurpleSnowIndex:
                    case Object.mineStoneRed1Index:
                    case Object.mineStoneRed2Index:
                    case Object.mineStoneBlue1Index:
                    case Object.mineStoneBlue2Index:
                    case Object.mineStoneGrey1Index:
                    case Object.mineStoneGrey2Index:
                    case Object.mineStoneBrown1Index:
                    case Object.mineStoneBrown2Index:
                    case Object.mineStoneMysticIndex:
                    case Object.mineStonePurpleIndex:
                    case 343:
                    case 450:
                        return BarrierType.Stone;
                    //case Object.mineStoneSnow1
                    case 674:
                    case 675:
                    case 784:
                    case 792:
                        return BarrierType.Weed;
                    default:
                        ModEntry.Log("obj: " + obj.Name +", id: "+ obj.ParentSheetIndex);
                        return BarrierType.UnknownObject;
                }
            }
            else
            {
                return BarrierType.Unknown;
            }
        }

        private static string FindBarrierDescription()
        {
            Vector2 nextPos = PlayerNextPosition();
            Vector2 nextTilePos = nextPos / Game1.tileSize;
            BarrierType barrierType = FindBarrierType();
            GameLocation location = Game1.currentLocation;
            switch (barrierType)
            {
                case BarrierType.Unknown:
                    return "";
                case BarrierType.UnknownObject:
                case BarrierType.Crop:
                case BarrierType.ActionObject:
                    if (location.getObjectAtTile((int)nextTilePos.X, (int)nextTilePos.Y) is StardewValley.Object obj && obj != null)
                        return obj.DisplayName;
                    else
                        return "";
                case BarrierType.UnknownTerrain:
                    return "";
                case BarrierType.UnknownLargeTerrain:
                    return "";
                case BarrierType.Wood:
                    return "stick";
                case BarrierType.LargeWood:
                    return "large log";
                case BarrierType.Stone:
                    return "rock";
                case BarrierType.LargeStone:
                    return "large rock";
                case BarrierType.Weed:
                    return "weed";
                case BarrierType.Bush:
                    return "bush";
                case BarrierType.Tree:
                    Tree tree = location.terrainFeatures[nextTilePos] as Tree;
                    string stage(Tree t)
                    {
                        if (t.stump.Value)
                            return "stump";

                        switch (t.growthStage.Value)
                        {
                            case Tree.seedStage: return "seed";
                            case Tree.saplingStage: return "sapling";
                            case Tree.sproutStage: return "sprout";
                            case Tree.bushStage: return "bush";
                            case Tree.treeStage:
                            default: 
                                return "";
                        }
                    }

                    //string 
                    string type(Tree t)
                    {
                        switch (tree.treeType.Value)
                        {
                            case Tree.bushyTree: return "oak"; 
                            case Tree.leafyTree: return "maple";
                            case Tree.palmTree: return "palm";
                            case Tree.pineTree: return "pine";
                            case Tree.winterTree1: return "winter1";
                            case Tree.winterTree2: return "winter2";
                            default: return "unknown";
                        }
                    }
                    string addon(Tree t) => t.tapped.Value ? "with tapper" : "";
                    return type(tree) + " tree " + stage(tree) + " " + addon(tree);
                case BarrierType.UnknownWall:
                    return "";
                case BarrierType.WoodWall:
                    return "wooden wall";
                case BarrierType.StoneWall:
                    return "stone wall";
                case BarrierType.Water:
                    return "water";
                case BarrierType.PetWaterBowl:
                    if (Game1.getFarm().petBowlWatered.Value)
                        return "filled water bowl";
                    else
                        return "empty water bowl";
                case BarrierType.Cliff:
                    return "cliff";
                case BarrierType.Fence:
                    return "fence";
                case BarrierType.Lantern:
                    return "lantern";
                case BarrierType.Housewall:
                    return "house wall";
                case BarrierType.Bench:
                    return "bench";
            }
            return "";
        }

        private static void PlaySoundForBarrier(BarrierType barrierType)
        {
            switch (barrierType)
            {
                case BarrierType.Wood: Game1.playSound("axe"); break; 
                case BarrierType.Stone: Game1.playSound("stoneCrack"); break;
                case BarrierType.Weed: Game1.playSoundPitched("leafrustle", 100); break;
                case BarrierType.WoodWall: Game1.playSound("woodWhack"); break;
                case BarrierType.Bush: Game1.playSound("leafrustle"); break;
                case BarrierType.Tree: Game1.playSound("axchop"); break;
                case BarrierType.Water: Game1.playSound("dropItemInWater"); break;
                case BarrierType.Lantern: Game1.playSound("clank"); break;
                case BarrierType.Fence: Game1.playSound("axe"); break;
                case BarrierType.Cliff: Game1.playSound("hammer"); break;
                case BarrierType.Housewall: Game1.playSound("treethud"); break;
                default: Game1.playSound("boop"); break;

            }
        }

        /// <summary>
        /// Returns the next position of the player while moving even if player is blocked.
        /// </summary>
        private static Vector2 PlayerNextPosition()
        {
            Rectangle boundingBox = Game1.player.GetBoundingBox();
            int hwidth = boundingBox.Width / 2;
            int hheight = boundingBox.Height / 2;
            boundingBox.X += hwidth;
            boundingBox.Y += hheight;
            int x = boundingBox.X - (boundingBox.X % Game1.tileSize);
            int y = boundingBox.Y - (boundingBox.Y % Game1.tileSize);
            switch (FacingDir)
            {
                case 0:
                    y -= Game1.tileSize;
                    break;
                case 1:
                    x += Game1.tileSize;
                    break;
                case 2:
                    y += Game1.tileSize;
                    break;
                case 3:
                    x -= Game1.tileSize;
                    break;
            }
            return new Vector2(x, y);
        }

        private static void ShowTileInfo(xTile.Dimensions.Location location)
        {
            ModEntry.Log("Checking Position [" + location.X + ", " + location.Y + "]");
            foreach (Layer layer in Game1.currentLocation.map.Layers)
            {
                ModEntry.Log("\tFound layer: " + layer.Id);
                //xTile.ObjectModel.PropertyValue value1;
                Tile tile = layer.PickTile(location, Game1.viewport.Size);
                if (tile != null)
                {
                    ModEntry.Log("\t\tFound tile: " + tile + ", properties:"+ (tile.Properties.Keys.Count == 0 ? " none" : ""));
                    //ModEntry.Log("\t\ttile properties:");
                    foreach (string s in tile.Properties.Keys)
                        ModEntry.Log("\t\t\t" + s);
                }
                else
                {
                    ModEntry.Log("\t\tno tile found");
                }
            }
        }
    }
}
