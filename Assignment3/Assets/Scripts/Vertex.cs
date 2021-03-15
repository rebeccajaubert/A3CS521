using System;
using System.Collections.Generic;
using UnityEngine;

public class Vertex : MonoBehaviour, IEquatable<Vertex>
{
    public Vector3 position;
    public Vertex prev;
    public Vertex next;
    public bool isGroundCorner = false;
    public float gCost;
    public float hCost;
    public float fCost
    {
        get { return gCost + hCost; }
    }
    public Vertex parent;

    public Vertex(Vector3 pos)
    {
        this.position = pos;
        gCost = 9999f;
        hCost = 9999f;
    }

    public Vertex(Vector3 pos, Vertex prev)
    {
        this.position = pos;
        this.prev = prev;
        gCost = 9999f;
        hCost = 9999f;
    }

    public override bool Equals(object obj)
    {
        Vertex objAsPart = obj as Vertex;
        if (objAsPart.isNull() && this.isNull())
        {
            Debug.Log("both null");
            return true;
        }
        else return Equals(objAsPart);
    }
    public override int GetHashCode()
    {
        return this.position.GetHashCode();
    }

    public  bool Equals(Vertex v2)
    {
        return position.Equals(v2.position);
    }

    public override string ToString()
    {
        return position.ToString();
    }

    public bool isNull()
    {
        return position == null;
    }

}

public class VertexComparer : IEqualityComparer<Vertex>
{
    public bool Equals(Vertex x, Vertex y)
    {
        return x.Equals(y);
    }

    public int GetHashCode(Vertex obj)
    {
        return obj.position.GetHashCode();
    }
}
