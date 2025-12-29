public class PlayerData
{
    [Save] public string Name { get; set; } = "Hero";
    [Save] public int Level { get; set; } = 1;
    [Save] public List<string> Inventory { get; set; } = new List<string>();

    public int TempHealth { get; set; } = 100; // Not saved
}

// In your code
var player = new PlayerData { Name = "Grok", Level = 50 };
player.TempHealth = 999; // ignored

await AutoSave.SaveAsync(player);

var loaded = await AutoSave.LoadAsync<PlayerData>();
// loaded.TempHealth == 0, others restored perfectly
