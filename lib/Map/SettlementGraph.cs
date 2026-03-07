using Dreamlands.Rules;

namespace Dreamlands.Map;

public class SettlementGraph
{
    public Dictionary<string, SettlementInfo> Settlements { get; }
    public string Root { get; }

    private SettlementGraph(Dictionary<string, SettlementInfo> settlements, string root)
    {
        Settlements = settlements;
        Root = root;
    }

    public SettlementInfo? GetParent(string id) =>
        Settlements.TryGetValue(id, out var info) && info.ParentId != null
            ? Settlements.GetValueOrDefault(info.ParentId)
            : null;

    public IReadOnlyList<SettlementInfo> GetChildren(string id) =>
        Settlements.TryGetValue(id, out var info)
            ? info.ChildIds.Select(c => Settlements[c]).ToList()
            : [];

    public int GetHopDistance(string fromId, string toId)
    {
        if (fromId == toId) return 0;

        var fromPath = PathToRoot(fromId);
        var toPath = PathToRoot(toId);

        if (fromPath == null || toPath == null) return -1;

        // Find lowest common ancestor
        var toSet = new HashSet<string>(toPath);
        for (int i = 0; i < fromPath.Count; i++)
        {
            if (toSet.Contains(fromPath[i]))
            {
                int j = toPath.IndexOf(fromPath[i]);
                return i + j;
            }
        }

        return -1;
    }

    public IReadOnlyList<SettlementInfo> GetSettlementsAtHop(string fromId, int hops) =>
        Settlements.Values.Where(s => s.Id != fromId && GetHopDistance(fromId, s.Id) == hops).ToList();

    public IReadOnlyList<SettlementInfo> GetSettlementsInBiome(Terrain terrain) =>
        Settlements.Values.Where(s => s.Biome == terrain).ToList();

    public static SettlementGraph Build(Map map)
    {
        var settlements = new Dictionary<string, SettlementInfo>();

        // Index all settlements by SettlementId
        foreach (var node in map.AllNodes())
        {
            if (node.Poi?.Kind != PoiKind.Settlement || node.Poi.SettlementId == null)
                continue;

            settlements[node.Poi.SettlementId] = new SettlementInfo
            {
                Id = node.Poi.SettlementId,
                Name = node.Poi.Name ?? "",
                X = node.X,
                Y = node.Y,
                Biome = node.Terrain,
                Tier = node.Region?.Tier ?? 1,
                Size = node.Poi.Size ?? SettlementSize.Village,
            };
        }

        // Wire parent/child from per-settlement Poi fields
        foreach (var info in settlements.Values)
        {
            var node = map[info.X, info.Y];
            var poi = node.Poi!;

            if (poi.TradeParentId != null)
                info.ParentId = poi.TradeParentId;

            if (poi.TradeChildIds != null)
            {
                foreach (var childId in poi.TradeChildIds)
                    info.ChildIds.Add(childId);
            }
        }

        var rootId = map.StartingCity?.Poi?.SettlementId
            ?? throw new InvalidOperationException("Map has no starting city with SettlementId");

        // Compute depth from root
        ComputeDepths(settlements, rootId);

        return new SettlementGraph(settlements, rootId);
    }

    private static void ComputeDepths(Dictionary<string, SettlementInfo> settlements, string rootId)
    {
        var queue = new Queue<string>();
        queue.Enqueue(rootId);
        if (settlements.TryGetValue(rootId, out var root))
            root.Depth = 0;

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            var info = settlements[id];
            foreach (var childId in info.ChildIds)
            {
                if (settlements.TryGetValue(childId, out var child))
                {
                    child.Depth = info.Depth + 1;
                    queue.Enqueue(childId);
                }
            }
        }
    }

    private List<string>? PathToRoot(string id)
    {
        var path = new List<string>();
        var current = id;
        var visited = new HashSet<string>();

        while (current != null)
        {
            if (!visited.Add(current)) return null; // cycle guard
            path.Add(current);
            if (current == Root) return path;
            current = Settlements.TryGetValue(current, out var info) ? info.ParentId : null;
        }

        return null; // disconnected
    }
}

public class SettlementInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required Terrain Biome { get; init; }
    public required int Tier { get; init; }
    public required SettlementSize Size { get; init; }
    public string? ParentId { get; set; }
    public List<string> ChildIds { get; } = [];
    public int Depth { get; set; }
}
