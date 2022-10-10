using System.Collections.Generic;
using UnityEngine;

namespace Assets.Map
{
    public class Corner
    {
        public int index;

        public Vector2 point;  // location
        public bool ocean;  // ocean
        public bool water;  // lake or ocean
        internal bool _marked; // Once this corner is marked with land or water, true.
        public bool coast;  // touches ocean and land polygons
        public bool land => !ocean && !water && !coast;
        public bool notLand => ocean || water || coast;
        public bool border;  // at the edge of the map
        public float elevation;  // 0.0-1.0
        public float moisture = 0;  // 0.0-1.0

        public List<Center> touches = new List<Center>();
        public List<Edge> protrudes = new List<Edge>();
        public List<Corner> adjacent = new List<Corner>();

        public int river;  // 0 if no river, or volume of water in river
        public Corner downslope;  // pointer to adjacent corner most downhill
        public Corner watershed;  // pointer to coastal corner, or null
        public int watershed_size;

        public Corner() {
            downslope = this;
            watershed = this;
        }

        //* 비교는 index가 정의된 같은 Map(Graph) 내에서만 수행하도록 합니다.
        public static bool operator ==(Corner a, Corner b) => a?.index == b?.index;
        public static bool operator !=(Corner a, Corner b) => a?.index != b?.index;
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
    }
}
