using System.Collections.Generic;
using UnityEngine;

namespace Assets.Map {
  public static class BiomeProperties {
    public static Dictionary<Biome, Color> Colors = new()
    {
      { Biome.Ocean, HexToColor("44447a") },
      //{ COAST, HexToColor("33335a") },
      //{ LAKESHORE, HexToColor("225588") },
      { Biome.Lake, HexToColor("336699") },
      //{ RIVER, HexToColor("225588") },
      { Biome.Marsh, HexToColor("2f6666") },
      { Biome.Ice, HexToColor("99ffff") },
      { Biome.Beach, HexToColor("a09077") },
      //{ BRIDGE, HexToColor("686860") },
      //{ LAVA, HexToColor("cc3333") },
      { Biome.Snow, HexToColor("ffffff") },
      { Biome.Tundra, HexToColor("bbbbaa") },
      { Biome.Bare, HexToColor("888888") },
      { Biome.Scorched, HexToColor("555555") },
      { Biome.Taiga, HexToColor("99aa77") },
      { Biome.Shrubland, HexToColor("889977") },
      { Biome.TemperatD, HexToColor("c9d29b") },
      { Biome.TempRainF, HexToColor("448855") },
      { Biome.TempDeciF, HexToColor("679459") },
      { Biome.Grassland, HexToColor("88aa55") },
      { Biome.SubTropiD, HexToColor("d2b98b") },
      { Biome.TropRainF, HexToColor("337755") },
      { Biome.TropSeasF, HexToColor("559944") }
    };

    const System.Globalization.NumberStyles hexStyle = System.Globalization.NumberStyles.HexNumber;
    static Color HexToColor(string hex) {
      byte r = byte.Parse(hex[ ..2], hexStyle);
      byte g = byte.Parse(hex[2..4], hexStyle);
      byte b = byte.Parse(hex[4.. ], hexStyle);
      return new Color32(r, g, b, 255);
    }
  }

  public enum Biome {
    Ocean,
    Marsh,
    Ice,
    Lake,
    Beach,
    Snow,
    Tundra,
    Bare,
    Scorched,
    Taiga,
    Shrubland,
    TemperatD, // Temperate Desert
    TempRainF, // Temperate Rain Forest
    TempDeciF, // Temperate Decidous Forest
    Grassland,
    TropRainF, // Tropical Rainy forest
    TropSeasF,   // Tropical Season Forest
    SubTropiD // Subtropical Desert
  }
}
