using DiscordRPC;
using DiscordRPC.Logging;
using FH5RP.Data;
using System;
using LogLevel = DiscordRPC.Logging.LogLevel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FH5RP.Net
{
    public class RPC
    {
        private static string ClientId = "909362638918668319";
        private static DiscordRpcClient Client { get; set; }
        static readonly HttpClient client = new HttpClient();
        private static int carID = 0;
        private static string carName = "";


        public static void FindNodes(JToken json, string name, string value, List<JToken> nodes)
        {
            if (json.Type == JTokenType.Object)
            {
                foreach (JProperty child in json.Children<JProperty>())
                {
                    if (child.Name == name && child.Value.ToString() == value)
                    {
                        nodes.Add(child);
                    }
                    FindNodes(child.Value, name, value, nodes);
                }
            }
            else if (json.Type == JTokenType.Array)
            {
                foreach (JToken child in json.Children())
                {
                    FindNodes(child, name, value, nodes);
                }
            }
        }
        public static void Initialize()
        {
            Client = new DiscordRpcClient(ClientId);
            Client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };
            Client.OnReady += (s, e) => Console.WriteLine($"[DiscordRPC] {s.ToString()} :: {e.Type} - {e.User} (ver: {e.Version})");
            Client.OnConnectionFailed += (s, e) => Console.WriteLine($"DiscordRPC connection failed.\n\t{e}");
            Client.Initialize();
        }

        public static async void UpdatePresence(TelemetryData data, Process[] process)
        {
            if (data is null) return;

            if (process is null)
            {
                process = Process.GetProcessesByName("ForzaHorizon5");
            }
            //Console.WriteLine($"cyl: {data.Engine.NumCylinders}, racetime: {data.TotalRaceTime} hp: {data.Power}, torq: {data.Torque}, boost: {data.Boost}, fuel: {data.Fuel}, dist: {data.DistanceTraveled}, race: {data.InRace}, time: {data.Timestamp}, lap: {data.LapNumber}, racepos: {data.RacePosition}, inrace: {data.InRace}");
            if (data.Vehicle.ID != carID)
            {
                //Console.WriteLine($"diff: data.Vehicle.ID");
                carID = data.Vehicle.ID;
                string responseBody = await client.GetStringAsync("https://raw.githubusercontent.com/ForzaMods/fh5idlist/main/README.md");
                //using HttpResponseMessage response = await client.GetAsync("https://airtable.com/shrSeJLd2KkV1FfDp/tbluQgnbWhDcOZYDB");
                //string responseBody = await client.
                //Console.WriteLine(responseBody);
                Regex rx = new Regex($@"\|\s([a-zA-Z0-9_ \-\#\.\\(\)\'\&\/]*)\s+\|\s{data.Vehicle.ID}\s+\|", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                Match match = rx.Match(responseBody);
                //Console.WriteLine($"Name: {match}");
                if (!match.Success)
                {
                    //Console.WriteLine($"null, {carID}, {data.Vehicle.ID}");
                    using (StreamReader read = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "Data", "FH5_Car_List_2022.json")))
                    {
                        //JObject json = JObject.Parse(dir);
                        string json = read.ReadToEnd();
                        var linq = JsonConvert.DeserializeObject<JToken>(json);
                        bool found = false;
                        foreach (var idx in linq) {
                            if (idx.Value<int>("ID") == data.Vehicle.ID)
                            {
                                found = true;
                                carName = idx.Value<string>("ShortName") ?? "";
                                //Console.WriteLine($"old {idx.Value<string>("ShortName")}, sla {idx.Value<string>("ShortName") ?? " "}, {idx}");
                            }
                            //if (idx.Value<int>("ID") == data.Vehicle.ID){ Console.WriteLine($"{idx}, true, {idx.Value<string>("ShortName")}");};
                            //Console.WriteLine($"idx: {idx}, {idx["ID"]}, {idx.Value<int>("ID")}, {idx.Value<int>("ID") == data.Vehicle.ID}");
                        }
                        if (!found)
                        {
                            carName = $"a {data.Engine.NumCylinders}-cylinder";
                        }
                        //var nodes = new List<JToken>();
                        //FindNodes(json, "MediaName", "POR_911GT2_95", nodes);
                        //var carname = id.Where(x => x.Equals(data.Vehicle.ID)).FirstOrDefault();
                        //Console.WriteLine($"id: {id}, name: {carname}");
                    }

                }
                else
                {
                    carName = match.Groups[1].Value.Trim();
                }
            }


            Client.SetPresence(new RichPresence()
            {
                State = $"Driving {carName} at {(int)data.GetMPH()} MPH ({(int)data.GetKPH()} KPH)",
                Details = "Exploring México",
                Timestamps = new Timestamps(){ Start = process[0].StartTime.ToUniversalTime() },
                Assets = new Assets()
                {
                    LargeImageKey = "logo",
                    SmallImageKey = $"carclass-{data.Vehicle.Index.ToString().ToLower()}",
                    SmallImageText = $"{data.Vehicle.Index.ToString()} | {data.Vehicle.PIValue} ({data.Vehicle.Drivetrain})"
                }
            });
        }
    }
}
