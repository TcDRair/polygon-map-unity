using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Maps {
  public static class BiomeExtensions {
    public static Color Color(this Biome biome) => HexToColor(biome switch {
      Biome.Ocean                   =>  "a09077", // "44447a",
      Biome.Lake                    =>  "336699",
      Biome.Marsh                   =>  "2f6666",
      Biome.Ice                     =>  "99ffff",
      Biome.Beach                   =>  "a09077",
      Biome.Snow                    =>  "ffffff",
      Biome.Tundra                  =>  "bbbbaa",
      Biome.Bare                    =>  "888888",
      Biome.Scorched                =>  "555555",
      Biome.Taiga                   =>  "99aa77",
      Biome.Shrubland               =>  "889977",
      Biome.TemperateDesert         =>  "c9d29b",
      Biome.TemperateRainyForest    =>  "448855",
      Biome.TemperateDecidousForest =>  "679459",
      Biome.Grassland               =>  "88aa55",
      Biome.SubtropicalDesert       =>  "d2b98b",
      Biome.TropicalRainyForest     =>  "337755",
      Biome.TropicalSeasonForest    =>  "559944",

      _ => "000000"
    });
    const System.Globalization.NumberStyles hexStyle = System.Globalization.NumberStyles.HexNumber;
    static Color HexToColor(string hex) {
      byte r = byte.Parse(hex[ ..2], hexStyle);
      byte g = byte.Parse(hex[2..4], hexStyle);
      byte b = byte.Parse(hex[4.. ], hexStyle);
      return new Color32(r, g, b, 255);
    }

    public static readonly int Length2Pow = 2 << (int)Mathf.Log(Enum.GetValues(typeof(Biome)).Cast<int>().Max(), 2); // 2^x (15 -> 16, 17 -> 32)
    public static float ToFloat(this Biome biome) => (float)biome / Length2Pow;
    public static Biome ToBiome(this float ratio) => (Biome)(int)(ratio * Length2Pow);
  }

  public enum Biome {
    /// <summary>빙하</summary>
    Ice,
    /// <summary>호수</summary>
    Lake,
    /// <summary>설원</summary>
    Snow,
    /// <summary>맨땅</summary>
    Bare,
    /// <summary>바다</summary>
    Ocean,
    /// <summary>습지 초원</summary>
    Marsh,
    /// <summary>해변</summary>
    Beach,
    /// <summary>타이가</summary>
    Taiga,
    /// <summary>툰드라</summary>
    Tundra,
    /// <summary>초원</summary>
    Grassland,
    /// <summary>황무지</summary>
    Scorched,
    /// <summary>관목지</summary>
    Shrubland,
    /// <summary>온대 사막</summary>
    TemperateDesert,
    /// <summary>아열대 사막</summary>
    SubtropicalDesert,
    /// <summary>열대 우림</summary>
    TropicalRainyForest,
    /// <summary>온대 우림</summary>
    TemperateRainyForest,
    /// <summary>열대 계절림</summary>
    TropicalSeasonForest,
    /// <summary>온대 낙엽 활엽수림</summary>
    TemperateDecidousForest,
  }
}
