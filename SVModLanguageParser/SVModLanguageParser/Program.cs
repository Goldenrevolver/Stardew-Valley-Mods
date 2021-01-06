using System;
using System.IO;

namespace SVModLanguageParser
{
    internal class Program
    {
        private static string cooking = "CookingRecipes.";

        private static string crafting = "CraftingRecipes.";

        private static string json = ".json";

        private static string output = "content.json";

        private static void Main()
        {
            string start = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "start.txt"));

            using (StreamWriter sw = File.CreateText(Path.Combine(Directory.GetCurrentDirectory(), output)))
            {
                sw.WriteLine(start);
            }

            // english
            GenerateEntry("", "", "", "", "", "", false, false, false);
            Console.WriteLine("English parsed");

            // custom spanish
            GenerateEntry("es-ES", "Hamburguesa", "de primavera", "de verano", "de otoño", "de invierno");
            Console.WriteLine("'es-ES' parsed");

            // custom chinese
            GenerateEntry("zh-CN", "救生汉堡", "(春季)", "(夏季)", "(秋季)", "(冬季)", false);
            Console.WriteLine("'zh-CN' parsed");

            string[] languages = new string[] { "de-DE", "fr-FR", "hu-HU", "it-IT", "jp-JP", "ko-KR", "pt-BR", "ru-RU", "tr-TR" };

            foreach (var item in languages)
            {
                string cookingPath = Path.Combine(Directory.GetCurrentDirectory(), cooking + item + json);
                string craftingPath = Path.Combine(Directory.GetCurrentDirectory(), crafting + item + json);

                if (File.Exists(cookingPath) && File.Exists(craftingPath))
                {
                    Parse(item, cookingPath, craftingPath);
                    Console.WriteLine($"'{item}' parsed");
                }
            }

            using (StreamWriter sw = File.AppendText(Path.Combine(Directory.GetCurrentDirectory(), output)))
            {
                sw.WriteLine("]}");
            }

            Console.WriteLine("Generation complete");
            Console.In.Read();
        }

        private static void Parse(string ending, string cookingPath, string craftingPath)
        {
            var text = File.ReadAllLines(cookingPath);

            string survivalBurger = null;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i].Contains("\"Survival Burger\""))
                {
                    var index1 = text[i].LastIndexOf('/');

                    survivalBurger = text[i].Substring(index1 + 1);

                    var index2 = survivalBurger.LastIndexOf('\"');

                    survivalBurger = survivalBurger.Substring(0, index2);
                }
            }

            text = File.ReadAllLines(craftingPath);

            string spring = null;
            string summer = null;
            string fall = null;
            string winter = null;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i].Contains("\"Wild Seeds (Sp)\""))
                {
                    var index1 = text[i].LastIndexOf('(');

                    spring = text[i].Substring(index1);

                    var index2 = spring.LastIndexOf(')');

                    spring = spring.Substring(0, index2 + 1);
                }
                else if (text[i].Contains("\"Wild Seeds (Su)\""))
                {
                    var index1 = text[i].LastIndexOf('(');

                    summer = text[i].Substring(index1);

                    var index2 = summer.LastIndexOf(')');

                    summer = summer.Substring(0, index2 + 1);
                }
                else if (text[i].Contains("\"Wild Seeds (Fa)\""))
                {
                    var index1 = text[i].LastIndexOf('(');

                    fall = text[i].Substring(index1);

                    var index2 = fall.LastIndexOf(')');

                    fall = fall.Substring(0, index2 + 1);
                }
                else if (text[i].Contains("\"Wild Seeds (Wi)\""))
                {
                    var index1 = text[i].LastIndexOf('(');

                    winter = text[i].Substring(index1);

                    var index2 = winter.LastIndexOf(')');

                    winter = winter.Substring(0, index2 + 1);
                }
            }

            GenerateEntry(ending, survivalBurger, spring, summer, fall, winter);
        }

        private static void GenerateEntry(string ending, string survivalBurger, string spring, string summer, string fall, string winter, bool addSpace = true, bool addSeperator = true, bool addDot = true)
        {
            string template = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "template.txt"));

            string dot = addDot ? "." : "";
            string space = addSpace ? " " : "";
            string seperator = addSeperator ? "/" : "";

            template = template.Replace("ENDING", dot + ending);
            template = template.Replace("SPRING", seperator + survivalBurger + space + spring);
            template = template.Replace("SUMMER", seperator + survivalBurger + space + summer);
            template = template.Replace("FALL", seperator + survivalBurger + space + fall);
            template = template.Replace("WINTER", seperator + survivalBurger + space + winter);

            using (StreamWriter sw = File.AppendText(Path.Combine(Directory.GetCurrentDirectory(), output)))
            {
                sw.WriteLine(template);
            }
        }
    }
}