using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Program
{
    public static void Main(string[] args)
    {
        DirectoryInfo directory = new DirectoryInfo(args[0]);
        //         YA              Name          W-E  Power
        Dictionary<int, Dictionary<string, Tuple<int, int>>> powerByYearsAgo = new();

        const int begin = -4752;
        const int end = 4014;

        foreach (FileInfo jsonFile in directory.EnumerateFiles("*.json"))
        {
            List<VectorLines> humans = ParseJson(jsonFile.OpenText().ReadToEnd());

            foreach (VectorLines human in humans)
            {
                // find minimum y value
                int minY = int.MaxValue;
                foreach (CivLine line in human.Lines)
                {
                    if (line.Points[1] < minY)
                    {
                        minY = line.Points[1];
                    }

                    if (line.Points[3] < minY)
                    {
                        minY = line.Points[3];
                    }
                }

                // find top line, if it exists
                CivLine? topLine = null;
                foreach (CivLine line in human.Lines)
                {
                    if (line.Points[1] == minY && line.Points[3] == minY) // flat, minimum line
                    {
                        topLine = line;
                        break;
                    }
                }

                // triangle case
                int widthTop = 0;
                int historyTop = minY;
                int westEast = -1000; // filter negative values below
                if (topLine != null)
                {
                    // top line case
                    widthTop = Math.Abs(topLine.Points[2] - topLine.Points[0]);
                    historyTop = topLine.Points[1];
                    westEast = Math.Min(topLine.Points[0], topLine.Points[2]) + 495; // cancel out shift in data to 0
                }

                historyTop -= end;
                historyTop = -(int)Math.Round(historyTop / ((double)(end - begin) / 3950));
                historyTop += 50;

                powerByYearsAgo.TryGetValue(historyTop, out Dictionary<string, Tuple<int, int>>? historyItem);
                if (historyItem == null)
                {
                    historyItem = new Dictionary<string, Tuple<int, int>>();
                    powerByYearsAgo.Add(historyTop, historyItem);
                }

                historyItem.Add(jsonFile.Name.Replace(".json", ""), new Tuple<int, int>(westEast, widthTop));
            }
        }
        
        Dictionary<int, Dictionary<string, Tuple<int, int>>> sortedPowerByYearsAgo = new();
        for (int yearsAgo = 100; yearsAgo <= 4000; yearsAgo += 50)
        {
            Dictionary<string, Tuple<int, int>> wedge = powerByYearsAgo[yearsAgo];
            Dictionary<string, Tuple<int, int>> copyWedge = new();
            
            List<int> sortedWE = wedge.OrderBy(x => x.Value.Item1).Select(x => x.Value.Item1).ToList();
            foreach (int i in sortedWE)
            {
                foreach (KeyValuePair<string, Tuple<int, int>> item in wedge)
                {
                    if(item.Value.Item1 < 0)
                    {
                        continue; // filter negative values
                    }
                    if (item.Value.Item1 == i && !copyWedge.ContainsKey(item.Key))
                    {
                        copyWedge.Add(item.Key, item.Value);
                    }
                }
            }
            
            sortedPowerByYearsAgo.Add(yearsAgo, copyWedge);
        }

        // serialize dictionary to json
        string json = JsonConvert.SerializeObject(sortedPowerByYearsAgo, Formatting.Indented);
        string path = Path.Combine(args[1], "powerByYearsAgo.json");
        File.WriteAllText(path, json);
        Console.WriteLine($"Wrote {path}");
    }

    static List<VectorLines> ParseJson(string json)
    {
        List<VectorLines> graphicObjects = [];
        JArray jsonArray = JArray.Parse(json);

        foreach (JToken itemArray in jsonArray)
        {
            VectorLines graphicObject = new VectorLines();
            foreach (JToken item in itemArray)
            {
                string property = ((JProperty)item.First).Name;
                JToken value = ((JProperty)item.First).Value;

                switch (property)
                {
                    case "opacity":
                        graphicObject.Opacity = value.ToObject<int>();
                        break;
                    case "fill":
                        graphicObject.Fill = value.ToObject<List<int>>();
                        break;
                    case "stroke":
                        graphicObject.Stroke = value.ToObject<List<int>>();
                        break;
                    case "strokeWidth":
                        graphicObject.StrokeWidth = value.ToObject<int>();
                        break;
                    case "line":
                        graphicObject.Lines.Add(new CivLine { Points = value.ToObject<List<int>>() });
                        break;
                }
            }

            graphicObjects.Add(graphicObject);
        }

        return graphicObjects;
    }

    [Serializable]
    class VectorLines
    {
        public int Opacity { get; set; }
        public List<int> Fill { get; set; }
        public List<int> Stroke { get; set; }
        public int StrokeWidth { get; set; }
        public List<CivLine> Lines { get; set; } = [];
    }

    class CivLine
    {
        public List<int> Points { get; set; }
    }
}