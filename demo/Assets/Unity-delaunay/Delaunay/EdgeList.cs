using UnityEngine;

namespace Delaunay
{
  internal sealed class EdgeList: Utils.IDisposable
  {
    private readonly float _deltaX;
    private readonly float _xMin;
    
    private readonly int _hashSize;
    private Halfedge[] _hash;
    public Halfedge LeftEnd { get; private set; }
    public Halfedge RightEnd { get; private set; }
    
    public void Dispose() {
      Halfedge prevHe;
      while (LeftEnd != RightEnd) {
        prevHe = LeftEnd;
        LeftEnd = LeftEnd.rightEdge;
        prevHe.Dispose();
      }
      LeftEnd = null;
      RightEnd.Dispose();
      RightEnd = null;

      for (int i = 0; i < _hashSize; ++i) _hash[i] = null;
      _hash = null;
    }
    
    public EdgeList(float xMin, float deltaX, int sqrt_nsites) {
      _xMin = xMin;
      _deltaX = deltaX;
      _hashSize = 2 * sqrt_nsites;

      _hash = new Halfedge[_hashSize];
      
      // two dummy Halfedges:
      LeftEnd = Halfedge.Dummy;
      RightEnd = Halfedge.Dummy;
      LeftEnd.leftEdge = null;
      LeftEnd.rightEdge = RightEnd;
      RightEnd.leftEdge = LeftEnd;
      RightEnd.rightEdge = null;
      _hash [0] = LeftEnd;
      _hash [_hashSize - 1] = RightEnd;
    }
    public void Insert (Halfedge lb, Halfedge newEdge) {
      newEdge.leftEdge = lb;
      newEdge.rightEdge = lb.rightEdge;
      lb.rightEdge.leftEdge = newEdge;
      lb.rightEdge = newEdge;
    }

    /**
     * This function only removes the Halfedge from the left-right list.
     * We cannot dispose it yet because we are still using it. 
     * @param halfEdge
     * 
     */
    public void Remove (Halfedge halfEdge)
    {
      halfEdge.leftEdge.rightEdge = halfEdge.rightEdge;
      halfEdge.rightEdge.leftEdge = halfEdge.leftEdge;
      halfEdge.edge = Edge.DELETED;
      halfEdge.leftEdge = halfEdge.rightEdge = null;
    }

    /**
     * Find the rightmost Halfedge that is still left of p 
     * @param p
     * @return 
     * 
     */
    public Halfedge EdgeListLeftNeighbor(Vector2 p)
    {
      int bucket;
      Halfedge halfEdge;
    
      /* Use hash table to get close to desired halfedge */
      bucket = Mathf.Clamp((int)((p.x - _xMin) / _deltaX * _hashSize), 0, _hashSize - 1);
      halfEdge = GetHash(bucket);

      int range = 1;
      while (halfEdge is null) halfEdge = GetHash(bucket - range) ?? GetHash(bucket + range++);
      
      /* Now search linear list of halfedges for the correct one */
      if (halfEdge == LeftEnd || (halfEdge != RightEnd && halfEdge.IsLeftOf (p))) {
        do {
          halfEdge = halfEdge.rightEdge;
        } while (halfEdge != RightEnd && halfEdge.IsLeftOf(p));
        halfEdge = halfEdge.leftEdge;
      } else {
        do {
          halfEdge = halfEdge.leftEdge;
        } while (halfEdge != LeftEnd && !halfEdge.IsLeftOf(p));
      }
    
      /* Update hash table and reference counts */
      if (bucket > 0 && bucket < _hashSize - 1) {
        _hash [bucket] = halfEdge;
      }
      return halfEdge;
    }

    /* Get entry from hash table, pruning any deleted nodes */
    private Halfedge GetHash(int b) {
      if (b < 0 || b >= _hashSize) return null;

      if (_hash[b] is var h && h?.edge == Edge.DELETED) return _hash[b] = null;
      return h;
    }
  }
}