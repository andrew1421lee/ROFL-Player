﻿using Newtonsoft.Json;
using Rofl.Reader.Models;
using Rofl.Reader.Utilities;
using Rofl.Requests;
using Rofl.Requests.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rofl.Main.Managers
{

    public class DetailWindowManager
    {
        /// <summary>
        /// Fill the Combo Box with player names
        /// </summary>
        /// <param name="data"></param>
        /// <param name="form"></param>
        public static void PopulatePlayerData(MatchMetadata data, Form form)
        {
            var playernames =
                from player in data.AllPlayers
                select player.SafeGet("NAME");

            form.BeginInvoke((Action)(() =>
            {
                ((ComboBox)form.Controls.Find("PlayerSelectComboBox", true)[0]).Items.AddRange(playernames.ToArray());
            }));
        }

        /// <summary>
        /// Fill out the list of player names and images. Set the victory text
        /// </summary>
        /// <param name="data"></param>
        /// <param name="form"></param>
        public static void PopulateGeneralReplayData(RequestManager requests, ReplayHeader data, Form form)
        {
            // Figure out which map the replay is for and download map image
            var map = data.InferredData.MapID;
            Task<ResponseBase> maptask = requests.MakeRequestAsync(new MapRequest()
            {
                MapID = map.ToString("D"),
                MapName = map.ToString("G")
            });

            form.BeginInvoke((Action)(async () =>
            {
                form.Controls.Find("GeneralGameVersionDataLabel", true)[0].Text = data.MatchMetadata.GameVersion;
                form.Controls.Find("GeneralGameMatchIDData", true)[0].Text = data.PayloadFields.MatchId.ToString();

                // Calculate game duration
                var time = ((decimal)(data.MatchMetadata.GameDuration / 1000) / 60);
                var minutes = (int)time;
                var seconds = (int)((time % 1.0m) * 60);
                form.Controls.Find("GeneralGameLengthDataLabel", true)[0].Text = $"{minutes} minutes and {seconds} seconds";

                // Find the map picturebox and set the tooltip
                var mapimg = (PictureBox)form.Controls.Find($"GeneralGamePictureBox", true)[0];
                new ToolTip().SetToolTip(mapimg, map.ToString());

                // Set the map image
                ResponseBase mapImageResponse = await maptask;

                if (!mapImageResponse.IsFaulted)
                {
                    mapimg.WaitOnLoad = false;
                    mapimg.Image = mapImageResponse.ResponseImage;
                }
                else
                {
                    mapimg.Image = mapimg.ErrorImage;
                }
            }));

            // Default victory text to draw
            string wongame = "No Contest";

            // If there are any blue players
            if (data.MatchMetadata.BluePlayers.ElementAt(0) != null)
            {
                // Since we're looking at blue players first, check who won
                if(data.MatchMetadata.BluePlayers.ElementAt(0).SafeGet("WIN").ToUpper() == "WIN")
                {
                    wongame = "Blue Victory";
                }
                else
                {
                    wongame = "Red Victory";
                }

                var counter = 1; // Counter used to match player number to UI views
                foreach (var player in data.MatchMetadata.BluePlayers)
                {
                    // Kick off task to download champion image
                    Task<ResponseBase> champImgTask = requests.MakeRequestAsync(new ChampionRequest()
                    {
                        ChampionName = player.SafeGet("SKIN")
                    });

                    form.BeginInvoke((Action)(async () => {
                        var namelabel = form.Controls.Find($"GeneralPlayerName{counter}", true)[0];
                        namelabel.Text = player.SafeGet("NAME");

                        // Set the tooltip for champion image
                        var champimg = (PictureBox)form.Controls.Find($"GeneralPlayerImage{counter}", true)[0];
                        new ToolTip().SetToolTip(champimg, player.SafeGet("SKIN"));

                        // Bold the name of the user
                        if (player.SafeGet("NAME").ToUpper() == RoflSettings.Default.Username.ToUpper())
                        {
                            namelabel.Font = new Font(namelabel.Font.FontFamily, namelabel.Font.Size, FontStyle.Bold);
                        }

                        counter++;

                        // Set the champion image
                        ResponseBase imgResponse = await champImgTask;

                        if (!imgResponse.IsFaulted)
                        {
                            champimg.WaitOnLoad = false;
                            champimg.Image = imgResponse.ResponseImage;
                        }
                        else
                        {
                            champimg.Image = champimg.ErrorImage;
                        }
                    }));
                }

                // Hide labels for extra player spots
                for(int i = data.MatchMetadata.BluePlayers.Count() + 1; i <= 6; i++)
                {
                    var namelabel = form.Controls.Find($"GeneralPlayerName{i}", true)[0];
                    namelabel.Visible = false;
                }
            }

            // If there are any red players
            if(data.MatchMetadata.RedPlayers.ElementAt(0) != null)
            {
                // Maybe there were no blue players, so lets see if red won (this seems redundant...)
                if (data.MatchMetadata.RedPlayers.ElementAt(0).SafeGet("WIN").ToUpper() == "WIN")
                {
                    wongame = "Red Victory";
                }
                else
                {
                    wongame = "Blue Victory";
                }

                var counter = 7; // Counter used to match player number to UI views
                foreach (var player in data.MatchMetadata.RedPlayers)
                {
                    // Kick off task to download champion image
                    Task<ResponseBase> champImgTask = requests.MakeRequestAsync(new ChampionRequest()
                    {
                        ChampionName = player.SafeGet("SKIN")
                    });

                    form.BeginInvoke((Action)(async () =>
                    {
                        var namelabel = form.Controls.Find($"GeneralPlayerName{counter}", true)[0];
                        namelabel.Text = player.SafeGet("NAME");

                        // Set the tooltip for champion image
                        var champimg = (PictureBox)form.Controls.Find($"GeneralPlayerImage{counter}", true)[0];
                        new ToolTip().SetToolTip(champimg, player.SafeGet("SKIN"));

                        // Bold the name of the user
                        if (player.SafeGet("NAME").ToUpper() == RoflSettings.Default.Username.ToUpper())
                        {
                            namelabel.Font = new System.Drawing.Font(namelabel.Font.FontFamily, namelabel.Font.Size, FontStyle.Bold);
                        }

                        counter++;

                        // Set the champion image
                        ResponseBase imgResponse = await champImgTask;

                        if (!imgResponse.IsFaulted)
                        {
                            champimg.WaitOnLoad = false;
                            champimg.Image = imgResponse.ResponseImage;
                        }
                        else
                        {
                            champimg.Image = champimg.ErrorImage;
                        }

                    }));
                }

                // Hide labels for extra player spots
                for (int i = data.MatchMetadata.RedPlayers.Count() + 7; i <= 12; i++)
                {
                    var namelabel = form.Controls.Find($"GeneralPlayerName{i}", true)[0];
                    namelabel.Visible = false;
                }

            }

            // We should know who won by now
            form.BeginInvoke((Action)(() => {
                form.Controls.Find("GeneralMatchWinnerLabel", true)[0].Text = wongame;
            }));
        }

        /// <summary>
        /// Fill out player stats
        /// </summary>
        /// <param name="player"></param>
        /// <param name="form"></param>
        public static void PopulatePlayerStatsData(RequestManager requests, Dictionary<string, string> player, Form form)
        {
            // We should already have downloaded the champion image, double check. Will return if we do.
            Task<ResponseBase> champImageTask = requests.MakeRequestAsync(new ChampionRequest()
            {
                ChampionName = player.SafeGet("SKIN")
            });

            // Setup tasks that will be used to download item images
            Task<ResponseBase>[] itemImageTasks = new Task<ResponseBase>[7];

            for (int taskCounter = 0; taskCounter < 7; taskCounter++)
            {
                itemImageTasks[taskCounter] = requests.MakeRequestAsync(new ItemRequest()
                {
                    ItemID = player.SafeGet("ITEM" + taskCounter)
                });
            }

            form.BeginInvoke((Action)(async () =>
            {
                ///// General Information
                PictureBox champimage = (PictureBox)form.Controls.Find("PlayerStatsChampImage", true)[0];

                // set champion image
                ResponseBase champResponse = await champImageTask;
                if (champResponse == null || champResponse.IsFaulted)
                {
                    champimage.Image = champimage.ErrorImage;
                }
                else
                {
                    champimage.WaitOnLoad = false;
                    champimage.Image = champResponse.ResponseImage;
                }

                // Set victory text
                var victorylabel = (TextBox)form.Controls.Find("PlayerStatswin", true)[0];
                if(player.SafeGet("WIN").ToUpper() == "FAIL")
                {
                    victorylabel.Text = "Defeat";
                    victorylabel.ForeColor = Color.Red;
                }
                else
                {
                    victorylabel.Text = "Victory!";
                    victorylabel.ForeColor = Color.Green;
                }

                ///// Champion, Level, KDA, CS
                var champlabel = (TextBox)form.Controls.Find("PlayerStatsChampName", true)[0];
                champlabel.Text = player.SafeGet("SKIN");

                var levellabel = (TextBox)form.Controls.Find("PlayerStatsChampLevel", true)[0];
                levellabel.Text = $"Level {player.SafeGet("LEVEL")}";

                var kdalabel = (TextBox)form.Controls.Find("PlayerStatsKDA", true)[0];
                kdalabel.Text = $"{player.SafeGet("CHAMPIONS_KILLED")} / {player.SafeGet("NUM_DEATHS")} / {player.SafeGet("ASSISTS")}";

                var cslabel = (TextBox)form.Controls.Find("PlayerStatsCreeps", true)[0];
                cslabel.Text = $"{player.SafeGet("MINIONS_KILLED")} CS";

                ///// Player Gold, Neutral Kills, Turrets
                var goldearnedlabel = (TextBox)form.Controls.Find("PlayerGoldEarned", true)[0];
                if(int.TryParse(player.SafeGet("GOLD_EARNED"), out int goldearned))
                {
                    goldearnedlabel.Text = goldearned.ToString("N0");
                }

                var goldspendlabel = (TextBox)form.Controls.Find("PlayerGoldSpent", true)[0];
                if (int.TryParse(player.SafeGet("GOLD_SPENT"), out int goldspent))
                {
                    goldspendlabel.Text = goldspent.ToString("N0");
                }

                var neutralkillslabel = (TextBox)form.Controls.Find("PlayerGoldNeutralCreeps", true)[0];
                neutralkillslabel.Text = player.SafeGet("NEUTRAL_MINIONS_KILLED");

                var towerskilledlabel = (TextBox)form.Controls.Find("PlayerGoldTowerKills", true)[0];
                towerskilledlabel.Text = player.SafeGet("TURRETS_KILLED");

                ///// Player Misc Stats Table

                var damagetochampslabel = (TextBox)form.Controls.Find("PlayerTotalDamageToChampions", true)[0];
                if (int.TryParse(player.SafeGet("TOTAL_DAMAGE_DEALT_TO_CHAMPIONS"), out int totaldamagetochamps))
                {
                    damagetochampslabel.Text = totaldamagetochamps.ToString("N0");
                }

                var damagetoobjlabel = (TextBox)form.Controls.Find("PlayerTotalDamageToObjectives", true)[0];
                if (int.TryParse(player.SafeGet("TOTAL_DAMAGE_DEALT_TO_OBJECTIVES"), out int totaldamagetoobjective))
                {
                    damagetoobjlabel.Text = totaldamagetoobjective.ToString("N0");
                }

                var damagetotowerlabel = (TextBox)form.Controls.Find("PlayerTotalDamageToTurrets", true)[0];
                if (int.TryParse(player.SafeGet("TOTAL_DAMAGE_DEALT_TO_TURRETS"), out int totaldamagetotower))
                {
                    damagetotowerlabel.Text = totaldamagetotower.ToString("N0");
                }

                var totaldamagelabel = (TextBox)form.Controls.Find("PlayerTotalDamageDealt", true)[0];
                if (int.TryParse(player.SafeGet("TOTAL_DAMAGE_DEALT"), out int totaldamage))
                {
                    totaldamagelabel.Text = totaldamage.ToString("N0");
                }

                var totalheallabel = (TextBox)form.Controls.Find("PlayerDamageHealed", true)[0];
                if (int.TryParse(player.SafeGet("TOTAL_HEAL"), out int totalheal))
                {
                    totalheallabel.Text = totalheal.ToString("N0");
                }

                var totaltakenlabel = (TextBox)form.Controls.Find("PlayerDamageTaken", true)[0];
                if (int.TryParse(player.SafeGet("TOTAL_DAMAGE_TAKEN"), out int totaltaken))
                {
                    totaltakenlabel.Text = totaltaken.ToString("N0");
                }

                var visionscorelabel = (TextBox)form.Controls.Find("PlayerVisionScore", true)[0];
                if (int.TryParse(player.SafeGet("VISION_SCORE"), out int visionscore))
                {
                    visionscorelabel.Text = visionscore.ToString("N0");
                }

                var wardsplacedlabel = (TextBox)form.Controls.Find("PlayerWardsPlaced", true)[0];
                if (int.TryParse(player.SafeGet("WARD_PLACED"), out int wardsplaced))
                {
                    wardsplacedlabel.Text = wardsplaced.ToString("N0");
                }

                ///// Player Inventory
                var allboxes = form.Controls.Find("PlayerSpellsItemsTable", true)[0].Controls;

                // Grab all item image boxes
                var itemboxes =
                    (from Control boxes in allboxes
                    where boxes.Name.Contains("PlayerItemImage")
                    select boxes).Cast<PictureBox>().ToArray();

                // Set item images
                for (int loadImageCounter = 0; loadImageCounter < 7; loadImageCounter++)
                {
                    var itemResponse = await itemImageTasks[loadImageCounter];
                    if(itemResponse.IsFaulted && itemResponse.Exception.Message.Equals("empty"))
                    {
                        itemboxes[loadImageCounter].Image = null;
                    }
                    else if (itemResponse.IsFaulted)
                    {
                        itemboxes[loadImageCounter].Image = itemboxes[loadImageCounter].ErrorImage;
                    }
                    else
                    {
                        itemboxes[loadImageCounter].WaitOnLoad = false;
                        itemboxes[loadImageCounter].Image = itemResponse.ResponseImage;
                    }
                }
            }));
        }

        /// <summary>
        /// Output all header data into a JSON file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static async Task<bool> WriteReplayHeaderToFile(string path, ReplayHeader header)
        {
            try
            {
                using (var writer = new StreamWriter(path))
                {
                    if(String.IsNullOrEmpty(header.RawJsonData))
                    {
                        await writer.WriteLineAsync(JsonConvert.SerializeObject(header));
                    } else
                    {
                        await writer.WriteLineAsync(header.RawJsonData);
                    }

                }
            }
            catch(Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Given region name (e.g. NA, EUW), return region endpoint name for URLs
        /// </summary>
        /// <param name="regionName"></param>
        /// <returns></returns>
        public static string GetRegionEndpointName(string regionName)
        {
            switch (regionName)
            {
                case "BR":
                    return "BR1";
                case "EUNE":
                    return "EUN1";
                case "EUW":
                    return "EUW1";
                case "JP":
                    return "JP1";
                case "KR":
                    return "KR";
                case "LAN":
                    return "LA1";
                case "LAS":
                    return "LA2";
                case "NA":
                    return "NA1";
                case "OCE":
                    return "OC1";
                case "TR":
                    return "TR1";
                case "RU":
                    return "RU";
                case "PBE":
                    return "PBE1";
                default:
                    return null;
            }
        }
    }
}
