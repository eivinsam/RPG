using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace RPG.Model
{
    [DataContract]
    public class NamedData : IComparable<NamedData>
    {
        [DataMember(Order = 0)] public string name = "";

        public int CompareTo(NamedData other) => name.CompareTo(other.name);
    }
    public class NamedList<T> : List<T>
        where T: NamedData
    {
        public T this[string name]
        {
            get => TryGetValue(name, out T entry) ? entry : throw new IndexOutOfRangeException();
            set => Add(value);
        }
        public new void Add(T entry)
        {
            for (int i = 0; i < Count; i++)
                if (this[i].name == entry.name)
                {
                    this[i] = entry;
                    return;
                }
            base.Add(entry);
        }
        public bool Remove(string name)
        {
            for (int i = 0; i < Count; i++)
                if (this[i].name == name)
                {
                    RemoveAt(i);
                    return true;
                }
            return false;
        }
        public bool TryGetValue(string name, out T value)
        {
            foreach (var entry in this)
                if (entry.name == name)
                {
                    value = entry;
                    return true;
                }
            value = null;
            return false;
        }
    }
    [DataContract]
    public class Item : NamedData
    {
        [DataMember(Order = 1)] public Dictionary<string, int> properties = new Dictionary<string, int>();
    }
    [DataContract]
    public class ItemInstance : NamedData
    {
        [DataMember(Order = 1)] public int quantity;
        [DataMember(Order = 2)] public int durability;

        public int MaxDurability
        {
            get
            {
                int value = 0;
                item?.properties.TryGetValue("Durability", out value);
                return value;
            }
        }

        public Item item;
    }
    [DataContract]
    public class Character : NamedData
    {
        public static readonly string[] stat_names = { "STR", "DEX", "NTE", "EMP", "NTU" };
        public static NameDistribution maleNames = new NameDistribution("GutterFornavnFodte.csv");
        public static NameDistribution femaleNames = new NameDistribution("JenterFornavnFodte.csv");

        public delegate Character Generator(Random rng);

        [DataMember(Order = 1)] public int body;
        [DataMember(Order = 2)] public int mind;
        [DataMember(Order = 3)] public readonly Dictionary<string, int> stats = new Dictionary<string, int>();
        [DataMember(Order = 4)] public readonly Dictionary<string, int> skills = new Dictionary<string, int>();
        [DataMember(Order = 5)] public readonly List<ItemInstance> items = new List<ItemInstance>();

        public int STR { get => stats["STR"]; set => stats["STR"] = value; }
        public int DEX { get => stats["DEX"]; set => stats["DEX"] = value; }
        public int NTE { get => stats["NTE"]; set => stats["NTE"] = value; }
        public int EMP { get => stats["EMP"]; set => stats["EMP"] = value; }
        public int NTU { get => stats["NTU"]; set => stats["NTU"] = value; }

        public int MaxBody => stats["STR"] + 8;
        public int MaxMind => stats["NTU"] + 8;


        public Character() { foreach (var name in stat_names) stats[name] = 0; }
        public Character(int str, int dex, int nte, int emp, int ntu) => SetStats(str, dex, nte, emp, ntu);

        public Character SetStats(int str, int dex, int nte, int emp, int ntu)
        {
            STR = str; DEX = dex;
            NTE = nte; EMP = emp;
            NTU = ntu;
            return this;
        }
        public Character NudgeStats(Random rng)
        {
            if (rng.Next(3) == 0) STR += rng.Next(-1, 2);
            if (rng.Next(3) == 0) DEX += rng.Next(-1, 2);
            if (rng.Next(3) == 0) NTE += rng.Next(-1, 2);
            if (rng.Next(3) == 0) EMP += rng.Next(-1, 2);
            if (rng.Next(3) == 0) NTU += rng.Next(-1, 2);
            return this;
        }
        public Character RandomName(Random rng, double pFemale)
        {
            name = rng.NextDouble() < pFemale ?
                femaleNames.Next(rng) :
                  maleNames.Next(rng);
            return this;
        }
        public Character FillVitals() { body = MaxBody; mind = MaxMind; return this; }

        internal void FillItemProperties(NamedList<Item> itemdata)
        {
            foreach (var i in items)
                if (itemdata.TryGetValue(i.name, out Item item))
                    i.item = item;
        }

        public static readonly Dictionary<string, Generator> generators = new Dictionary<string, Generator>
        {
            ["Average"] = rng => new Character(5, 5, 5, 5, 5).NudgeStats(rng).RandomName(rng, 0.5),
            ["Brute"] = rng => new Character(8, 5, 4, 4, 4).NudgeStats(rng).RandomName(rng, 0.05),
            ["Inn patron"] = rng => new Character(5, 4, 5, 6, 4).NudgeStats(rng).RandomName(rng, 0.3)
        };
    }

    [DataContract]
    public class Place : NamedData
    {
        [DataMember(Order = 1)] public NamedList<Character> characters = new NamedList<Character>();
    }


    [DataContract]
    public class Model
    {
        private static readonly DataContractJsonSerializerSettings json_settings = 
            new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };

        [DataMember] public Place party = new Place { name = "Party" };
        [DataMember] public NamedList<Place> places = new NamedList<Place>();
        [DataMember] public NamedList<Item> items = new NamedList<Item>();

        public static Model Read(string filename)
        {
            try
            {
                var result = new DataContractJsonSerializer(typeof(Model), json_settings)
                    .ReadObject(new FileStream(filename, FileMode.Open, FileAccess.Read)) as Model;

                foreach (var c in result.party.characters)
                    c.FillItemProperties(result.items);
                foreach (var p in result.places)
                    foreach (var c in p.characters)
                        c.FillItemProperties(result.items);

                return result;
            }
            catch (FileNotFoundException)
            {
                return new Model();
            }
        }

        public void Write(string filename)
        {
            new DataContractJsonSerializer(typeof(Model), json_settings)
                .WriteObject(new FileStream(filename, FileMode.Create, FileAccess.Write), this);
        }

    }


}
