namespace Zoneora.Core;

public sealed class ZoneManager
{
    private readonly List<ZoneModel> zones;

    public ZoneManager(IEnumerable<ZoneModel>? initialZones = null)
    {
        zones = initialZones?.ToList() ?? [];
    }

    public IReadOnlyList<ZoneModel> Zones => zones;

    public ZoneModel Create(string name)
    {
        int index = zones.Count;
        ZoneModel zone = new()
        {
            Name = string.IsNullOrWhiteSpace(name) ? "New zone" : name.Trim(),
            GridColumn = 1 + (index % 3) * 4,
            GridRow = 1 + (index / 3) * 3
        };
        zones.Add(zone);
        return zone;
    }

    public bool Remove(Guid id)
    {
        ZoneModel? zone = zones.FirstOrDefault(candidate => candidate.Id == id);
        return zone is not null && zones.Remove(zone);
    }
}
