using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

// 1つのアクション。.acf の JSON 形式と同じキー名を使う（旧版の .acf と互換）
class MacroAction
{
    public MacroAction()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    [JsonPropertyName("id")] public string Id { get; set; }             // 結果を参照するための項目の識別子
    [JsonPropertyName("type")] public string Type { get; set; }         // click / drag / hold / keyboard / wait / repeat / repeat_end / if / else / if_end / calc / round / convert / input / setvar / addvar / getvar / cond
    [JsonPropertyName("btn")] public string Btn { get; set; }
    [JsonPropertyName("mode")] public string Mode { get; set; }         // keyboard の入力方式（text / hotkey）
    [JsonPropertyName("value")] public string Value { get; set; }
    [JsonPropertyName("x")] public int? X { get; set; }
    [JsonPropertyName("y")] public int? Y { get; set; }
    [JsonPropertyName("sx")] public int? Sx { get; set; }
    [JsonPropertyName("sy")] public int? Sy { get; set; }
    [JsonPropertyName("ex")] public int? Ex { get; set; }
    [JsonPropertyName("ey")] public int? Ey { get; set; }
    [JsonPropertyName("duration")] public int? Duration { get; set; }
    [JsonPropertyName("waypoints")] public List<int[]> Waypoints { get; set; }
    [JsonPropertyName("count")] public int? Count { get; set; }         // 繰り返す回数
    [JsonPropertyName("op")] public string Op { get; set; }             // 計算・条件の演算子
    [JsonPropertyName("number")] public int? Number { get; set; }       // 旧版の条件分岐の比べる値（互換用）
    [JsonPropertyName("a")] public ValueRef A { get; set; }             // 左の値（計算・条件・端数・型変換・変数）
    [JsonPropertyName("b")] public ValueRef B { get; set; }             // 右の値（計算・条件）
    [JsonPropertyName("var")] public string Var { get; set; }           // 変数名（変数の操作・保存先）
    [JsonPropertyName("place")] public int? Place { get; set; }         // 端数を処理：位（10 の何乗か）
    [JsonPropertyName("rmode")] public string RMode { get; set; }       // 端数を処理：round / floor / ceil
    [JsonPropertyName("target")] public string Target { get; set; }     // 型変換の変換先：string / int / decimal / bool
    [JsonPropertyName("prompt")] public string Prompt { get; set; }     // 入力のプロンプト
    [JsonPropertyName("multiline")] public bool? Multiline { get; set; } // 入力：複数行を許可
    [JsonPropertyName("default")] public ValueRef Default { get; set; } // 入力：デフォルトの値
}

// 値の参照：数値（文字で保存）・変数・ほかの項目の結果
class ValueRef
{
    [JsonPropertyName("kind")] public string Kind { get; set; }        // lit / var / res
    [JsonPropertyName("text")] public string Text { get; set; }        // lit の値
    [JsonPropertyName("name")] public string Name { get; set; }        // var の名前
    [JsonPropertyName("ref")] public string Ref { get; set; }          // res の項目の Id

    public static ValueRef Literal(string text) => new ValueRef { Kind = "lit", Text = text };
    public static ValueRef Variable(string name) => new ValueRef { Kind = "var", Name = name };
    public static ValueRef Result(string id) => new ValueRef { Kind = "res", Ref = id };
}

// 実行を止める理由（警告として表示する）
class MacroError : Exception
{
    public MacroError(string message) : base(message) { }
}
