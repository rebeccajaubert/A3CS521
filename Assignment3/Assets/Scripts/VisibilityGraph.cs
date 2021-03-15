
using System;
using System.Collections.Generic;
using UnityEngine;

public class VisibilityGraph : MonoBehaviour
{
    private GameObject terrain;
   
    public Vertex[][] verticesTerrain = new Vertex[Main.nbAlcoves + 4][]; //+4 for the 4 ground vertices (saved as [pos,pos,pos,pos] bool isGround=true)
    public Vertex[][] reflexVerticesTerrain = new Vertex[Main.nbAlcoves][];
    public Vertex[][] reflexVerticesObstacles = new Vertex[Main.nbObstacles][];
    public Dictionary<Vertex, List<Vertex>> graph = new Dictionary<Vertex, List<Vertex>>(new VertexComparer());

    public VisibilityGraph(GameObject _terrain, GameObject[] obstacles) {
        this.terrain = _terrain;
        storeTerrainVertices();
        storeObstaclesVertices(obstacles);
        //createBoundary();
        createReducedVisibilityGraph();
    }



    private void createReducedVisibilityGraph()
    {
        bool firstIteration = true;   
        foreach(Vertex[] obs in reflexVerticesObstacles)
        {
            foreach (Vertex v1 in obs)
            {
                //endpoints same object
                addEdge(v1, v1.prev);

                //obstacles with obstacles
                foreach (Vertex[] obs2 in reflexVerticesObstacles)
                {
                    if (obs.Equals(obs2)) continue; //same obs

                    foreach (Vertex v2 in obs2)
                    {
                        if (!isVisible(v1, v2)) continue; //is v2 visible from v

                        if (bitangent(v1, v2))
                        {
                            addEdge(v1, v2);
                        }

                    }

                }

                //obs with terrain
                foreach (Vertex[] tVertices in reflexVerticesTerrain)
                {
                    foreach (Vertex v2 in tVertices)
                    {
                        if(firstIteration) addEdge(v2, v2.prev);    //endpoints of alcove

                        if (!isVisible(v1, v2)) continue;
                        //I need to consider prev and next of v2 to be the ones saved in verticesTerrain
                        Vertex _v2 = Array.Find(tVertices, ele => ele.Equals(v2));

                        if (bitangent(v1, _v2))
                        {
                            addEdge(v1, v2);    
                        }
                    }
                }

            }
            firstIteration = false;
        }

        foreach(Vertex[] vertices in verticesTerrain)
        {
            foreach(Vertex v in vertices)
            {
                addEdge(v, v.prev); //terrain with terrain : endpoints same object
            }
        }
      

    }

    //not used = I ended up setting the boundaries statically bc adding a collider to a linerenderer is a struggle
    private void createBoundary()
    {

        for (int i = 0; i < verticesTerrain.Length; i++)
        {
            for (int j = 0; j < verticesTerrain[i].Length; j++)
            {
                Vertex v = verticesTerrain[i][j];
                if (v.isGroundCorner == true && j != 3) continue;
                if (v.isNull()) continue;   // shouldn't happen

                //add to graph to avoid an another loop through verticesTerrain
               // addEdge(v, v.prev); //terrain with terrain : endpoints same object

                //create boundary with invible objects with layer Boundary
                GameObject bound = Instantiate(Resources.Load("Prefabs/Line", typeof(GameObject))) as GameObject;
                LineRenderer boundRdr = bound.GetComponent<LineRenderer>();
                BoxCollider boundCol = bound.AddComponent<BoxCollider>();
                // boundRdr.enabled = false;
                boundRdr.startColor = Color.cyan; boundRdr.endColor = Color.magenta;
                bound.layer = LayerMask.NameToLayer("Boundary");
                boundRdr.SetPosition(0, v.prev.position);
                boundRdr.SetPosition(1, v.position);
                boundCol.transform.position = new Vector3((v.prev.position.x - v.position.x) / 2f
                                                         , (v.prev.position.y - v.position.y) / 2f,
                                                           (v.prev.position.z - v.position.z) / 2f);
                boundCol.size = new Vector3(Vector3.Distance(v.prev.position, v.position), 1f, 0.2f);

            }
        }
    }

    public void addEdge(Vertex v1, Vertex v2)
    {
        if (!graph.ContainsKey(v1)) addVertex(v1);
        if (!graph.ContainsKey(v2)) addVertex(v2);
        if (!graph[v1].Contains(v2)) graph[v1].Add(v2);
        if (!graph[v2].Contains(v1)) graph[v2].Add(v1);
    }

    private void addVertex(Vertex v)
    {
        graph.Add(v, new List<Vertex>());
    }

    public static bool isVisible(Vertex v1, Vertex v2)
    {
        Vector3 raycastDir1 = v2.position - v1.position;
        Vector3 raycastDir2 = v1.position - v2.position;
        bool intersectCollider = (!Physics.Raycast(v1.position, raycastDir1, Vector3.Distance(v1.position, v2.position), LayerMask.GetMask("Obstacle"))
            && !Physics.Raycast(v2.position, raycastDir2, Vector3.Distance(v2.position, v1.position), LayerMask.GetMask("Obstacle")));
        bool notOnGround = Physics.Raycast(v1.position, raycastDir1, Vector3.Distance(v1.position, v2.position), LayerMask.GetMask("Boundary"))
                           && Physics.Raycast(v2.position, raycastDir2, Vector3.Distance(v2.position, v1.position), LayerMask.GetMask("Boundary"));
      
        return intersectCollider && !notOnGround;
    }

    public void drawEdgesGraph(Color color)   
    {
        foreach(Vertex k in graph.Keys)
        {
            foreach (Vertex v in graph[k])
            {
                drawLineHelper(k, v, color, terrain.transform);
            }
        }
    }

    public void drawLineHelper(Vertex a, Vertex b, Color color, Transform parent)
    {
        GameObject edgePrefab = Resources.Load("Prefabs/Line") as GameObject;
        GameObject edge = Instantiate(edgePrefab, parent);

        LineRenderer edgeRdr = edge.GetComponent<LineRenderer>();
        edgeRdr.startColor =color; edgeRdr.endColor = color;
        edgeRdr.SetPosition(0, a.position);
        edgeRdr.SetPosition(1, b.position);
    }

    
    //this code is from the correction of quizz1
    private bool bitangent(Vertex a, Vertex b)
    {
        if ((((SAT(a.position, b.position, b.prev.position) > 0) == (SAT(a.position, b.position, b.next.position) > 0))
            || SAT(a.position, b.position, b.next.position) == 0 || SAT(a.position, b.position, b.prev.position) == 0)
            && (((SAT(a.position, b.position, a.prev.position) > 0) == (SAT(a.position, b.position, a.next.position) > 0))
            || SAT(a.position, b.position, b.next.position) == 0 || SAT(a.position, b.position, b.prev.position) == 0))
        {
            return true;
        }

        //Vector3 v = new Vector3(b.position.x - a.position.x, b.position.y - a.position.y, b.position.z - a.position.z); // vector from a to b
        //Vertex p1 = new Vertex( a.position + v * 2);// pick a point extending v past b
        //Vertex p2 = new Vertex (b.position - v * 2); // pick a point extending v past a
        //if (isVisible(a, p1) && isVisible(b, p2))
        //{
        //    return true;
        //}
        return false;
    }
    

    //signed area of a triangle ABC
    private float SAT(Vector3 a, Vector3 b, Vector3 c)
    {
        float signedA =(float) 1/2 * (-a.z*b.x + a.x*b.z + a.z*c.x - b.z*c.x - a.x*c.z +b.x*c.z );
        
        return signedA ;

    }

    //store all vertices in verticesMatrix. Store reflex vertices in reflexVerticesTerrain
    private void storeTerrainVertices()
    {
        GameObject ground = terrain.transform.GetChild(0).gameObject;
        //ground vertices
        Vector3[] verticesG = ground.GetComponent<MeshFilter>().mesh.vertices;
        

        //alcoves counter
        int i = 0; int t = i;
        // find index of reflex corners
        int reflexC = 0; int reflexC2 = 2;
        foreach (Transform child in ground.transform)
        {
            Vector3[] vertices = child.GetComponent<MeshFilter>().mesh.vertices; //24 vertices bc 6 faces
            verticesTerrain[t] = new Vertex[4]; //init
            reflexVerticesTerrain[i] = new Vertex[2];
            int k = 0;
            for (int j = 0; j < 4; j++)
            {
                Vector3 positionCorner;
                

                //reflex vertices
                if (j == reflexC || j == reflexC2)
                {
                    Vertex reflexVertex;
                    positionCorner = child.TransformPoint(vertices[8 + j]);

                    if (i == 0 && k == 0) reflexVertex = new Vertex(positionCorner);
                    else
                    {
                        if (k == 0)
                        {
                            reflexVertex = new Vertex(positionCorner, reflexVerticesTerrain[i - 1][1]); //previous alcove last vertex
                            reflexVerticesTerrain[i - 1][1].next = reflexVertex;
                        }
                        else
                        {
                            reflexVertex = new Vertex(positionCorner, reflexVerticesTerrain[i][k - 1]); //same alcove first vertex
                            reflexVerticesTerrain[i][k - 1].next = reflexVertex;
                        }

                    }

                    reflexVerticesTerrain[i][k] = reflexVertex;
                    k++;
                }

                //all vertices
                Vertex vertex;
                if (j == 0 || j == 1) positionCorner = child.TransformPoint(vertices[8 + j]); //only take up face //it stores first top right corner then top left, bottom right, bottom left
                else { positionCorner = child.TransformPoint(vertices[8 + (j + 1) % 2 + 2]); }

                //insert ground Vertex
                if (i == 2 && j==0){
                    vertex = new Vertex(ground.transform.TransformPoint(verticesG[8 + 3]), verticesTerrain[t - 1][3]);
                    vertex.isGroundCorner = true;
                    verticesTerrain[t - 1][3].next = vertex;  //set next of previous alcove
                    for (int c=0;c<4;c++)    verticesTerrain[t][c] = vertex; 
                    t++;
                    verticesTerrain[t] = new Vertex[4]; //init
                }
                else if (i == 5 && j == 0)
                {
                    vertex = new Vertex(ground.transform.TransformPoint(verticesG[8 + 2]), verticesTerrain[t - 1][3]); //TODO possible rotate ground
                    vertex.isGroundCorner = true;
                    verticesTerrain[t - 1][3].next = vertex;  //set next of previous alcove
                    for (int c = 0; c < 4; c++) verticesTerrain[t][c] = vertex;
                    t++;
                    verticesTerrain[t] = new Vertex[4]; //init
                }
                else if (i == 7 && j == 0)
                {
                    vertex = new Vertex(ground.transform.TransformPoint(verticesG[8 + 0]), verticesTerrain[t - 1][3]);
                    vertex.isGroundCorner = true;
                    verticesTerrain[t - 1][3].next = vertex;  //set next of previous alcove
                    for (int c = 0; c < 4; c++) verticesTerrain[t][c] = vertex;
                    t++;
                    verticesTerrain[t] = new Vertex[4]; //init
                }


                if (i == 0 && j == 0)
                {
                    //insert last ground vertex
                    vertex = new Vertex(ground.transform.TransformPoint(verticesG[8 + 1])); 
                    vertex.isGroundCorner = true;
                    for (int c = 0; c < 4; c++) verticesTerrain[t][c] = vertex; //the other Vertex in this array will be null
                    t++;
                    verticesTerrain[t] = new Vertex[4]; //init
                    vertex = new Vertex(positionCorner, verticesTerrain[t-1][0]);
                }
                else
                {
                    if (j == 0)
                    {
                        vertex = new Vertex(positionCorner, verticesTerrain[t - 1][3]);
                        verticesTerrain[t - 1][3].next = vertex;
                    }
                    else
                    {
                        vertex = new Vertex(positionCorner, verticesTerrain[t][j - 1]);
                        verticesTerrain[t][j - 1].next = vertex;
                    }
                }

                verticesTerrain[t][j] = vertex;
                

            }
            t++;
            i++;
        }
        //set prev of first vertex to be the last. set next of last to be first
        reflexVerticesTerrain[0][0].prev = reflexVerticesTerrain[Main.nbAlcoves-1][1];
        reflexVerticesTerrain[Main.nbAlcoves-1][1].next = reflexVerticesTerrain[0][0];

        verticesTerrain[0][0].prev = verticesTerrain[Main.nbAlcoves+3][3];
        verticesTerrain[Main.nbAlcoves+3][3].next = verticesTerrain[0][0];

    }

    //create red cubes on terrain vertices of terrain and magenta cubes for reflex obstacles vertices
    public void showVertices()
    {
        for (int i = 0; i < verticesTerrain.Length; i++)
        {
            foreach (Vertex corner in verticesTerrain[i])
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = corner.position ;
                cube.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                cube.GetComponent<Renderer>().material.color = Color.red;
            }
        }

        for (int i = 0; i < reflexVerticesObstacles.Length; i++)
        {
            foreach (Vertex corner in reflexVerticesObstacles[i])
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = corner.position;
                cube.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                cube.GetComponent<Renderer>().material.color = Color.magenta;
            }
        }
    }

    private void storeObstaclesVertices(GameObject[] obstacles)
    {
        int i = 0;
        foreach (GameObject obs in obstacles)
        {
            //store reflex vertices
            Transform vPartInstantiated = obs.transform.GetChild(0).GetChild(0);     //VertiL
            Transform hPartInstantiated = vPartInstantiated.GetChild(0).GetChild(0);   //HorizLSprite
            Vector3[] vertiVertices = vPartInstantiated.GetComponent<MeshFilter>().mesh.vertices;
            Vector3[] horizVertices = hPartInstantiated.GetComponent<MeshFilter>().mesh.vertices; //get the sprite of horiz part
            reflexVerticesObstacles[i] = new Vertex[5];   //init
            int k = 0;
            //vertical L part
            for (int j = 0; j < 4; j++)
            {
                if (j == 0) continue;
                Vertex vertex = null;
                //only consider vertices of bottom face of the obstable
                if (k==0)  vertex = new Vertex(vPartInstantiated.TransformPoint(vertiVertices[12 + j])); //first vertex => no prev 
                else {
                    vertex = new Vertex(vPartInstantiated.TransformPoint(vertiVertices[12 + j]), reflexVerticesObstacles[i][k-1]);
                    reflexVerticesObstacles[i][k - 1].next = vertex;
                }
                reflexVerticesObstacles[i][k] = vertex;
                k++;
            }
            
            //horizontal L part
            Vertex vH1 = new Vertex(hPartInstantiated.TransformPoint(vertiVertices[12]), reflexVerticesObstacles[i][k-1]);
            Vertex vH2 = new Vertex(hPartInstantiated.TransformPoint(vertiVertices[13]), vH1);
            reflexVerticesObstacles[i][k - 1].next = vH1;
            vH1.next = vH2;
            vH2.next = reflexVerticesObstacles[i][0]; //set next of last to be first
            reflexVerticesObstacles[i][k] = vH1;
            reflexVerticesObstacles[i][k + 1] = vH2;

            //set prev of first vertex to be the last. 
            reflexVerticesObstacles[i][0].prev = vH2;

            i++;
        }
    }

}
