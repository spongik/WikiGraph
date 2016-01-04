using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace WikiGraph
{
    class Program
    {
        static ConnectionMultiplexer RedisConnection;
        static IDatabase RedisDb;

        static Dictionary<string, double> GetChildrenWeights(List<string> words, double weight = 1.0, int depth = 2)
        {
            var result = new Dictionary<string, double>();

            if (depth <= 0)
            {
                return result;
            }

            words.ForEach(word => result.Add(word, weight));

            foreach (var word in words)
            {
                foreach (var pair in GetChildrenWeights(GetChildren(word), weight * .5, depth - 1))
                {
                    if (result.ContainsKey(pair.Key))
                    {
                        result[pair.Key] = Math.Max(result[pair.Key], pair.Value);
                    }
                    else
                    {
                        result.Add(pair.Key, pair.Value);
                    }
                }
            }

            return result;
        }

        static List<string> GetChildren(string input)
        {
            return RedisDb.SetMembers(input)
                .Select(val => val.ToString())
                .ToList();
        }

        static void Main(string[] args)
        {
            List<string> words = new List<string>();

            Console.WriteLine("Enter words to search, one per line:");

            string word;
            while (!String.IsNullOrEmpty(word = Console.ReadLine()))
            {
                words.Add(word);
            }
            
            RedisConnection = ConnectionMultiplexer.Connect("localhost");
            RedisDb = RedisConnection.GetDatabase();

            Console.WriteLine("Result:");

            var found = new List<KeyValuePair<string, double>>();

            foreach (var item in words)
            {
                var children = GetChildren(item);

                if (children.Count == 0)
                {
                    Console.WriteLine("Warning: '{0}' not found", item);
                }
                else
                {
                    found.AddRange(GetChildrenWeights(children));
                }
            }

            foreach (var item in found.GroupBy(item => item.Key).Select(group => new {
                Word = group.Key,
                Weight = group.Select(item => item.Value).Sum(),
                Repeats = group.Count()
            }).OrderBy(item => item.Weight))
            {
                if (item.Repeats > 1)
                {
                    Console.WriteLine("{0}: {1}, {2}", item.Word, item.Repeats, item.Weight);
                }
            }
        }
    }
}
