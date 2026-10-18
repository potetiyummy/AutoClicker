using System.Collections.Generic;
using System.Text.Json.Serialization;

// 1つのアクション。.acf の JSON 形式と同じキー名を使う（旧版の .acf と互換）
class MacroAction
{
    [JsonPropertyName("type")] public string Type { get; set; }          // click / drag / hold / keyboard / wait
    [JsonPropertyName("btn")] public string Btn { get; set; }            // left_click など
    [JsonPropertyName("mode")] public string Mode { get; set; }          // text / hotkey
    [JsonPropertyName("value")] public string Value { get; set; }
    [JsonPropertyName("x")] public int? X { get; set; }
    [JsonPropertyName("y")] public int? Y { get; set; }
    [JsonPropertyName("sx")] public int? Sx { get; set; }
    [JsonPropertyName("sy")] public int? Sy { get; set; }
    [JsonPropertyName("ex")] public int? Ex { get; set; }
    [JsonPropertyName("ey")] public int? Ey { get; set; }
    [JsonPropertyName("duration")] public int? Duration { get; set; }
    [JsonPropertyName("waypoints")] public List<int[]> Waypoints { get; set; }
    [JsonPropertyName("count")] public int? Count { get; set; }          // 繰り返し（repeat）の回数
}
