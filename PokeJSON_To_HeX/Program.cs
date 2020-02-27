using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;


namespace PokeJSON_To_HeX
{
    class Program
    {
        static string PK_HEXFormatFilter = "=Box={0}\n=Slot={1}\n";        
        static string PK_HEXFormatData = ".PID={0}\n.Nature={1}\n";
        static string PK_HEXFormatIVStats = ".IV_ATK={0}\n.IV_DEF={1}\n.IV_HP={2}\n.IV_SPA={3}\n.IV_SPD={4}\n.IV_SPE={5}\n";
        static string PK_HEXEndLine = ";";
        static string PK_HEXGen3UData = ".EncryptionConstant={0}\n";

        static int _Generation = 3, maxPerFile = 32 * 30, fileGenCount = 1;
        static bool _orderByTotalStats = true, _findSpecific = false, _findAllTopNatures = true, _findAllOneFile = true, removeDupes = true;
        static string _natureToFind = "hardy";

        static void Main(string[] args)
        {
            string fileLoc = Directory.GetCurrentDirectory() + "\\PIDs.json";
            string outputFile = Directory.GetCurrentDirectory() + "\\HeX_{0}.txt";
            string jsonData = LoadData(fileLoc);

            List<List<PokemonItem>> pokemonLists;

            //Kill Extra Data
            jsonData = jsonData.Substring(jsonData.IndexOf("\n") + 1);
            jsonData = jsonData.Substring(0, jsonData.LastIndexOf("\n "));

            //Split Strings per mon and generate List of Pokemon
            string[] mons = jsonData.Replace("\"", "").Split("},\n");
            List<PokemonItem> pokemons = GeneratePokemon(mons);

            if (_findAllTopNatures)
            {
                if (!_findAllOneFile)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        List<PokemonItem> sorted = pokemons.FindAll(x => x._stats._nature.Equals(i.ToString()));
                        sorted = sorted.OrderByDescending(x => x.GetTotalStats).ToList();
                        pokemonLists = BreakOutLists(sorted);

                        string outputFileOverride = outputFile.Replace(".txt", "_nature_" + i + ".txt");

                        WriteOutHeX(pokemonLists, outputFileOverride);
                    }
                }
                else
                {
                    List<PokemonItem> sortedAll = new List<PokemonItem>();
                    for (int i = 0; i < 25; i++)
                    {
                        List<PokemonItem> sorted = pokemons.FindAll(x => x._stats._nature.Equals(i.ToString()));
                        sorted = sorted.OrderByDescending(x => x.GetTotalStats).ToList();

                        sortedAll.AddRange(sorted.GetRange(0, Math.Min(30, sorted.Count)));
                        
                    }
                    pokemonLists = BreakOutLists(sortedAll);
                   
                    WriteOutHeX(pokemonLists, outputFile);
                }
            }
            else
            {
                if (_orderByTotalStats)
                    pokemons = pokemons.OrderByDescending(x => x.GetTotalStats).ToList();
                if (_findSpecific)
                    pokemons = pokemons.FindAll(x => x._stats._nature.Equals(ConvertNature(_natureToFind).ToString()));

                pokemonLists = BreakOutLists(pokemons);
                
                WriteOutHeX(pokemonLists, outputFile);
            }
            
            //Write out text
           
        }


        static List<List<PokemonItem>> BreakOutLists(List<PokemonItem> pokemons)
        {
            List<List<PokemonItem>> pokemonLists = new List<List<PokemonItem>>();
            for (int i = 0; i < pokemons.Count; i += maxPerFile)
            {
                pokemonLists.Add(pokemons.GetRange(i, Math.Min(maxPerFile, pokemons.Count - i)));
            }
            return pokemonLists;
        }

        static string LoadData(string fileLoc)
        {
            string json = "";
            using (StreamReader r = new StreamReader(fileLoc))
            {
                json = r.ReadToEnd();
            }
            return json;            
        }

        static void WriteOutHeX(List<List<PokemonItem>> pokemonLists, string fileLoc)
        {
            for (int c = 0; c < pokemonLists.Count; c++)
            {
                List<PokemonItem> pokemons = pokemonLists[c];
                if ((c > 0 && c == fileGenCount) || pokemons.Count <= 0)
                    break;

                using (StreamWriter w = new StreamWriter(string.Format(fileLoc, c)))
                {
                    for (int i = 0; i < pokemons.Count; i++)
                    {
                        int boxID = (i / 30) + 1;
                        int slotID = (i % 30) + 1;
                        string outStr = "";

                        outStr += string.Format(PK_HEXFormatFilter, boxID, slotID);
                        outStr += string.Format(PK_HEXFormatData, pokemons[i]._stats._PID, pokemons[i]._stats._nature);
                        if (_Generation == 3)
                        {
                            outStr += string.Format(PK_HEXGen3UData, pokemons[i]._stats._PID);
                        }
                        outStr += string.Format(PK_HEXFormatIVStats, pokemons[i]._stats._atk, pokemons[i]._stats._def, pokemons[i]._stats._hp, pokemons[i]._stats._spa, pokemons[i]._stats._spd, pokemons[i]._stats._spe);

                        //MUST DO
                        outStr += PK_HEXEndLine;

                        if (i < pokemons.Count - 1)
                            outStr += "\n";

                        w.Write(outStr);
                    }
                }
            }
        }

        static List<PokemonItem> GeneratePokemon(string[] mons)
        {
            List<PokemonItem> pokemons = new List<PokemonItem>();
          
            //Make Pokemon
            for (int i = 0; i < mons.Length; i++)
            {
                int buffer = 0;
                PokemonItem pi = new PokemonItem();
                pi._stats = new PokeStats();

                pi._ID = mons[i].Substring(0, mons[i].IndexOf(":")).Trim();
                mons[i] = mons[i].Substring(mons[i].IndexOf("\n") + 1);

                string[] stats = mons[i].Split("\n");
                for (int j = 0; j < stats.Length; j++)
                {
                    string stat = stats[j].Trim().Replace(",", "");
                    string[] statSplit = stat.Split(":");

                    if (statSplit.Length < 2)
                        continue;

                    if (statSplit[0].ToLower().Trim() == "frame")
                    {
                        buffer++;
                        continue;
                    }

                    if (statSplit[0].ToLower().Trim() == "nature")
                    {
                        statSplit[1] = ConvertNature(statSplit[1].ToLower().Trim()).ToString();
                    }
                    else if (statSplit[0].ToLower().Trim() == "pid")
                    {
                        uint pidInt = uint.Parse(statSplit[1], System.Globalization.NumberStyles.HexNumber);
                        statSplit[1] = pidInt.ToString();
                    }

                    pi._stats[j-buffer] = statSplit[1].Trim();
                }

                if(removeDupes)
                {
                    if (!pokemons.Any(x => x._stats._PID == pi._stats._PID))
                    {
                        pokemons.Add(pi);
                    }
                }
                else
                    pokemons.Add(pi);
            }

            return pokemons;
        }

        static int ConvertNature(string type)
        {
            switch (type)
            {
                case "hardy":
                    return 0;
                case "lonely":
                    return 1;
                case "brave":
                    return 2;
                case "adamant":
                    return 3;
                case "naughty":
                    return 4;
                case "bold":
                    return 5;
                case "docile":
                    return 6;
                case "relaxed":
                    return 7;
                case "impish":
                    return 8;
                case "lax":
                    return 9;
                case "timid":
                    return 10;
                case "hasty":
                    return 11;
                case "serious":
                    return 12;
                case "jolly":
                    return 13;
                case "naive":
                    return 14;
                case "modest":
                    return 15;
                case "mild":
                    return 16;
                case "quiet":
                    return 17;
                case "bashful":
                    return 18;
                case "rash":
                    return 19;
                case "calm":
                    return 20;
                case "gentle":
                    return 21;
                case "sassy":
                    return 22;
                case "careful":
                    return 23;
                case "quirky":
                    return 24;
            }

            //random
            return 25;
        }

        [System.Serializable]
        public class PokemonItem
        {
            public string _ID;
            public PokeStats _stats;

            public int GetTotalStats
            {
                get
                {
                    int total = 0;
                    for (int i = 4; i <= 9; i++)
                    {
                        total += int.Parse(_stats[i]);
                    }
                    return total;
                }
            }
        }

        [System.Serializable]
        public class PokeStats
        {
            public string _PID;
            public string _extra;
            public string _nature;
            public string _ability;
            public string _hp;
            public string _atk;
            public string _def;
            public string _spa;
            public string _spd;
            public string _spe;
            public string _hidden;
            public string _power;
            public string _gender;

            public string _fuckNuggets;

            public string this[int key]
            {
                get{ return ArrHelper(key); }
                set{ ArrHelper(key) = value; }
            }

            ref string ArrHelper(int key)
            {
                switch(key)
                {
                    case 0:
                        return ref _PID;
                    case 1:
                        return ref _extra;
                    case 2:     
                        return ref _nature;
                    case 3:     
                        return ref _ability;
                    case 4:     
                        return ref _hp;
                    case 5:     
                        return ref _atk;
                    case 6:     
                        return ref _def;
                    case 7:     
                        return ref _spa;
                    case 8:     
                        return ref _spd;
                    case 9:     
                        return ref _spe;
                    case 10:    
                        return ref _hidden;
                    case 11:    
                        return ref _power;
                    case 12:   
                        return ref _gender;
                }
                return ref _fuckNuggets;
            }

        }
    }
}
