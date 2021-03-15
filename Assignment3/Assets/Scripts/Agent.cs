using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

public class Agent : MonoBehaviour, IEquatable<Agent>
{
    private readonly static float speed = 0.1f;
    public static List<int> helperPositionAvailable = new List<int>();

    public Color color;
    private GameObject terrain;
    public GameObject agentObj;
    public GameObject dest;
    public bool notAtDestination = true;
    public VisibilityGraph visibilityGraph;
    private List<Vertex> currentPath;
    private Vertex currentGoal;
    public int failedAttempts = 0;

    public Agent(GameObject _terrain, Color _color,  VisibilityGraph reducedVisibilityGraph)
    {
        this.terrain = _terrain;
        GameObject a = Resources.Load("Prefabs/Agent") as GameObject;
        this.agentObj = Instantiate(a) as GameObject;
        color = _color;
        agentObj.GetComponent<Renderer>().material.color = _color;
        Vector3 pos = this.spawnedPosition();
        agentObj.transform.position = pos;
        this.visibilityGraph = reducedVisibilityGraph;    
        //add edges from agent to visible reflex vertices
        addEdgesFromPosToGraph(this.getPos());
        setDestination();
        currentPath = Astar();
        currentGoal = currentPath[0];
        currentPath.Remove(currentGoal);
    }

    public Agent(GameObject _terrain, Color _color, VisibilityGraph reducedVisibilityGraph, Vector3 position)
    {
        this.terrain = _terrain;
        GameObject a = Resources.Load("Prefabs/Agent") as GameObject;
        this.agentObj = Instantiate(a) as GameObject;
        color = _color;
        agentObj.GetComponent<Renderer>().material.color = _color;
        agentObj.transform.position = position;
        this.visibilityGraph = reducedVisibilityGraph;
        addEdgesFromPosToGraph(this.getPos());
        setDestination();
        currentPath = Astar();
        currentGoal = currentPath[0];
        currentPath.Remove(currentGoal);
    }

    public Vector3 getPos()
    {
        return agentObj.transform.position;
    }

    public void moveWithAStar()
    {
        Main.planningWatch.Start();
        agentObj.GetComponent<Collider>().isTrigger = true; //if collides with agents

        //reached destination
        if (Mathf.Abs(this.getPos().x - this.dest.transform.position.x) <= 0.05f &&
            Mathf.Abs(this.getPos().z - this.dest.transform.position.z) <= 0.05f)
        {
            Main.nbPlansSuccessful++;
            notAtDestination = false;
            return;
        }
        //reached vertex on path => go to next one
        if ( Mathf.Abs(currentGoal.position.x - this.getPos().x) <=0.05f &&
            Mathf.Abs(currentGoal.position.z - this.getPos().z) <=0.05f  )
        {
            if (currentPath.Count == 0)
            {
                Main.nbPlansSuccessful++;
                notAtDestination = false;
                return;
            }
            currentGoal = currentPath[0];
            currentPath.Remove(currentGoal);
        }
        Vector3 direction =  currentGoal.position - this.getPos();
        
        this.agentObj.transform.position += speed*direction.normalized;

        agentObj.GetComponent<Collider>().isTrigger = false;
        Main.planningWatch.Stop();
    }

    //agent collide with agents : this collider.istrigger = true; otherAgents.collider.isTirgger = false. but still doesn't notice
    private void OnTriggerEnter(Collider col)
    {
        Main.planningWatch.Start();
        if (col.gameObject.CompareTag("Agent"))
        {
            Main.nbReplanning++;
            StartCoroutine(waitBeforReplanning());
        }
        Main.planningWatch.Stop();
    }

    private IEnumerator waitBeforReplanning()
    {
        yield return new WaitForSeconds(2);
        failedAttempts++;
        currentPath = Astar();
        Main.planningWatch.Start();
        currentGoal = currentPath[0];
        currentPath.Remove(currentGoal);
        Main.planningWatch.Stop();
    }

    //For vizualisation use agent.drawPath() to visualize path agent is walking on
    public void drawPath()
    {
        Vertex prev = new Vertex(this.getPos());
        foreach (Vertex v in currentPath)
        {
            if (v.Equals(new Vertex(this.getPos()))) continue;
            visibilityGraph.drawLineHelper(prev, v, Color.black, agentObj.transform);
            prev = v;

        }
    }

    private List<Vertex> Astar()
    {
        Main.planningWatch.Start();

        List<Vertex> path = new List<Vertex>();
        Vertex agentVertex = new Vertex(this.getPos());
        Vertex destinationVertex = new Vertex(this.dest.transform.position);
        agentVertex.gCost = 0f;
        agentVertex.hCost = Vector3.Distance(agentVertex.position, destinationVertex.position);
    
        List<Vertex> openSet = new List<Vertex>();
        HashSet<Vertex> closedSet = new HashSet<Vertex>();
        openSet.Add(agentVertex);

        while(openSet.Count > 0)
        {
            Vertex v = openSet[0];

            for(int i=1; i<openSet.Count; i++)
            {
                if(openSet[i].fCost <= v.fCost)
                {
                    if (openSet[i].hCost < v.hCost) v = openSet[i];
                }
            }

            openSet.Remove(v);
            closedSet.Add(v);

            if (v.Equals(destinationVertex))
            {
                path = RetracePath(agentVertex, v);
                break;
            }
            foreach (Vertex neighbour in this.visibilityGraph.graph[v])
            {
                if (closedSet.Contains(neighbour)) continue;

                float newCostToNeighbour = v.gCost + Vector3.Distance(v.position, neighbour.position);

                if (v.Equals(agentVertex)) neighbour.gCost = Vector3.Distance(neighbour.position, agentVertex.position);

                if ( newCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newCostToNeighbour;
                    neighbour.hCost = Vector3.Distance(neighbour.position, destinationVertex.position);
                    neighbour.parent = v;

                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                }
            }

        }
        Main.nbPlans++;
        Main.planningWatch.Stop();
        return path;
    }

    private List<Vertex> RetracePath(Vertex agent, Vertex dest)
    {
        List<Vertex> path = new List<Vertex>();
        Vertex v = dest;

        while (!v.Equals(agent))
        {
            path.Add(v);
            v = v.parent;
        }
        path.Reverse();
        return path;

    }

    private Vector3 spawnedPosition()
    {
        Transform terrainSpawnedOn;
        System.Random rnd = new System.Random();
        bool collide = true;
        Vector3 pos = Vector3.zero;
        int randTerrain=0;
        while (collide)
        {
            //randomly choose ground or alcove
            int rand = rnd.Next(0, helperPositionAvailable.Count-1);
            randTerrain = helperPositionAvailable[rand];
            if (randTerrain >= 10) terrainSpawnedOn = terrain.transform.GetChild(0);    //ground
            else
            { //alcove
                terrainSpawnedOn = terrain.transform.GetChild(0).GetChild(randTerrain);
            }   


            Vector3 terrainLevel = terrainSpawnedOn.position + new Vector3(0, terrainSpawnedOn.localScale.y / 2, 0);

            int signX = rnd.Next(0, 2) == 0 ? 1 : -1; int signZ = rnd.Next(0, 2) == 0 ? 1 : -1;
            float randomX = signX * (UnityEngine.Random.value * terrainSpawnedOn.localScale.x / 2);
            float randomZ = signZ * (UnityEngine.Random.value * terrainSpawnedOn.localScale.z / 2);
            Vector3 randPos = new Vector3(randomX, 0, randomZ);
            pos = terrainLevel + randPos;
            if (!isOverlappingObj(pos)) collide = false;
        }
        helperPositionAvailable.Remove(randTerrain);    //no other agent can be on this alcove
        return pos;
    }

    private bool isOverlappingObj(Vector3 pos)
    {
        // for an unknown reason doesn't notice other agent colliders when perfect collision but won't happen cause not on same alcove
        Collider[] colliders = Physics.OverlapCapsule(pos + new Vector3(0,0,agentObj.transform.localScale.z/2),
                                                       pos - new Vector3(0, 0, agentObj.transform.localScale.z /2),
                                                        this.agentObj.transform.localScale.x/2);
        
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Obstacle") || col.CompareTag("Agent") ) //maybe add boundary layer check : add collider to boundary Line renderer
            {
                return true;
            }
        }
        return false;
    }

    public void setDestination()
    {
        Vector3 _dest = spawnedPosition();
        GameObject destObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        destObj.transform.position = _dest;
        destObj.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
        destObj.GetComponent<Renderer>().material.color = this.color;
        this.dest = destObj;

        addEdgesFromPosToGraph(_dest);  //edges with terrain
        Vertex d = new Vertex(_dest);   Vertex a = new Vertex(this.getPos());
        if (VisibilityGraph.isVisible(d, a))   //edge with agent if possible
        {
            this.visibilityGraph.addEdge(d, a);
            visibilityGraph.drawLineHelper(d, a, this.color, agentObj.transform);
        }

    }

    private void addEdgesFromPosToGraph(Vector3 pos)
    {
        Vertex positionVertex = new Vertex(pos);
        //obstacles
        foreach(Vertex[] vertices in this.visibilityGraph.reflexVerticesObstacles)
        {
            foreach(Vertex v in vertices)
            {
                if (VisibilityGraph.isVisible(positionVertex, v))
                {
                    visibilityGraph.addEdge(positionVertex, v);
                    visibilityGraph.drawLineHelper(positionVertex, v, this.color, agentObj.transform);
                }
            }
        }
        //terrain
        foreach (Vertex[] vertices in this.visibilityGraph.reflexVerticesTerrain)
        {
            foreach (Vertex v in vertices)
            {
                if (VisibilityGraph.isVisible(positionVertex, v))
                {
                    visibilityGraph.addEdge(positionVertex, v);
                    visibilityGraph.drawLineHelper(positionVertex, v, this.color, agentObj.transform);
                }
            }
        }
    }

    public override string ToString()
    {
        return getPos().ToString();
    }

    public override bool Equals(object obj)
    {
        Vertex objAsPart = obj as Vertex;
        if (objAsPart.isNull() && this.isNull())
        {
            return true;
        }
        else return Equals(objAsPart);
    }
    public override int GetHashCode()
    {
        return this.getPos().GetHashCode();
    }

    public bool Equals(Agent a2)
    {
        return getPos().Equals(a2.getPos());
    }
    public bool isNull()
    {
        return getPos() == null;
    }
}