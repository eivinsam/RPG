using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using System.Diagnostics;

namespace RPG
{
    public class NameDistribution
    {
        struct Item : IComparable<Item>
        {
            public string name;
            public int cprob;

            public int CompareTo(Item other)
            {
                return cprob - other.cprob;
            }
        }

        readonly List<Item> names = new List<Item>();

        public NameDistribution(string filename)
        {
            var pattern = new Regex("\"[^\"]+\" [0-9]\\.[0-9][0-9][0-9]");

            int sum = 0;

            Debug.WriteLine("trying to open '" + filename + "' in '" + System.Environment.CurrentDirectory + "'");
            foreach (var line in File.ReadAllLines(filename))
            {
                if (!pattern.IsMatch(line))
                    continue;
                var split = line.Split(new char[] { ' ', '.' });
                var f = int.Parse(split[1]) * 1000 + int.Parse(split[2]);
                if (f > 0)
                {
                    sum += f;
                    names.Add(new Item { name = split[0].Substring(1, split[0].Length - 2), cprob = sum });
                }
            }
        }

        public string Next(Random rng)
        {
            int n = rng.Next(names[names.Count-1].cprob);
            int i = names.BinarySearch(new Item { cprob = n });
            return names[i > 0 ? i : ~i].name;
        }
    }


}
