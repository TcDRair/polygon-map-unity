using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Maps {
  [Serializable] public class Biome : SuperEnum<BiomeEnum> {
    public Color Color => HexToColor(Value switch {
      BiomeEnum.Ocean =>  "44447a",
      BiomeEnum.Lake =>  "336699",
      BiomeEnum.Marsh =>  "2f6666",
      BiomeEnum.Ice =>  "99ffff",
      BiomeEnum.Beach =>  "a09077",
      BiomeEnum.Snow =>  "ffffff",
      BiomeEnum.Tundra =>  "bbbbaa",
      BiomeEnum.Bare =>  "888888",
      BiomeEnum.Scorched =>  "555555",
      BiomeEnum.Taiga =>  "99aa77",
      BiomeEnum.Shrubland =>  "889977",
      BiomeEnum.TemperateDesert =>  "c9d29b",
      BiomeEnum.TemperateRainyForest =>  "448855",
      BiomeEnum.TemperateDecidousForest =>  "679459",
      BiomeEnum.Grassland =>  "88aa55",
      BiomeEnum.SubtropicalDesert =>  "d2b98b",
      BiomeEnum.TropicalRainyForest =>  "337755",
      BiomeEnum.TropicalSeasonForest =>  "559944",

      _ => "000000"
    });
    public static Color ToColor(BiomeEnum be) => ((Biome)be).Color;
    const System.Globalization.NumberStyles hexStyle = System.Globalization.NumberStyles.HexNumber;
    static Color HexToColor(string hex) {
      byte r = byte.Parse(hex[ ..2], hexStyle);
      byte g = byte.Parse(hex[2..4], hexStyle);
      byte b = byte.Parse(hex[4.. ], hexStyle);
      return new Color32(r, g, b, 255);
    }

    public float Ratio => (int)Value / (float)Length2Pow;
    #region Operators
    public static implicit operator Biome(BiomeEnum E) => new() { Value = E };
    public static explicit operator Biome(int value) => new() { Value = (BiomeEnum)value };
    public static explicit operator Biome(float ratio) => (Biome)Mathf.RoundToInt(ratio * Length2Pow);
    public static bool operator ==(Biome a, Biome b) => a.Value == b.Value;
    public static bool operator !=(Biome a, Biome b) => a.Value != b.Value;
    public override bool Equals(object other) => other is Biome b && Value == b.Value;
    public override int GetHashCode() => Value.GetHashCode();
    #endregion
  }
  public enum BiomeEnum {
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
