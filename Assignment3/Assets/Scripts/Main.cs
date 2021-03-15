
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    public GameObject agentPanel;
    public InputField inputField;
    public Text panelText;

    public readonly static int nbAlcoves = 10;
    public readonly static int nbObstacles = 4;
    public static int nbAgents = 4;

    public GameObject terrain;
    public GameObject[] obstacles = new GameObject[nbObstacles];
    private VisibilityGraph VG;
    private  List<Agent> agents = new List<Agent>();

    public static int nbPlans = 0;
    public static int nbReplanning = 0;
    public static int nbPlansSuccessful = 0;
    public static Stopwatch planningWatch = new Stopwatch();


    private IEnumerator quitGameAfterNseconds(int n)
    {
        yield return new WaitForSeconds(n);
        EditorApplication.isPlaying = false;
    }

    //At start wait for player to input nbAgents: when click onConfirm it will call getAgentsButton()
    public void getAgentsButton()
    {
        string nbAgentText = inputField.text;
        bool isNumeric = int.TryParse(nbAgentText, out int n);
        if (isNumeric)
        {
            nbAgents = n;
            agentPanel.SetActive(false);
            setUpLevel();
            //quit game after 30 seconds
            StartCoroutine(quitGameAfterNseconds(30));
        }
        else
        {
            panelText.text = "Invalid number of agents, please enter a valid integer";
        }
    }

    private void OnApplicationQuit()
    {
        if (false)  //set to true to write to file
        {
            string path = @"/Users/rebeccajaubert/Downloads/resultsPathFinding.txt";
            // This text is added only once to the file.
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("nbAgents  nbPlans   nbReplanning    nbPlansSuccessful   planningTime");

                }
            }

            // add info
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine(nbAgents + "         " + nbPlans + "                " + nbReplanning + "                  " + nbPlansSuccessful + "                  " + planningWatch.ElapsedMilliseconds);

            }

            // Open the file to read from.
            using (StreamReader sr = File.OpenText(path))
            {
                string s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    Console.WriteLine(s);
                }
            }
        }
        //UnityEngine.Debug.Log(nbPlans);
        //UnityEngine.Debug.Log(nbReplanning);
        //UnityEngine.Debug.Log(nbPlansSuccessful);
        //UnityEngine.Debug.Log(planningWatch.ElapsedMilliseconds);
    }

    private void setUpLevel()
    {
        InstantiateObstacles(nbObstacles);
        VG = new VisibilityGraph(terrain, obstacles);

        VG.showVertices();
        VG.drawEdgesGraph(Color.red);
        createAgents(VG);
    }

    private void Update()
    {
        //Debug.Log(agents.Count);//TODO issue why is 0 when agent sleft
        for(int i=0; i<agents.Count; i++)
        {
            Agent a = agents[i];
            if(a.notAtDestination) a.moveWithAStar();
            else if(!a.notAtDestination && !a.isNull() || a.failedAttempts>3)
            {
                StartCoroutine(waitBeforeNewDest(a));
            }
            else { UnityEngine.Debug.Log("null ? " + a.ToString()); }

        }
        
    }

    private IEnumerator waitBeforeNewDest(Agent a)
    {
        //if no more positions avaliable for destinations reset positions available
        if (Agent.helperPositionAvailable.Count == 1)
        {
            Agent.helperPositionAvailable.Clear();
            for (int i = 0; i < nbAgents * 5; i++)
            {
                Agent.helperPositionAvailable.Add(i);
            }
        }
        //new agent but looking exact same
        Agent cloneAgent = new Agent(terrain, a.color, VG, a.getPos());
        
        if (!a.isNull())
        {
            agents.Remove(a);
            Destroy(a.agentObj);
            Destroy(a.dest);
        }
        //wait 3 secondes
        yield return new WaitForSeconds(3);
        agents.Add(cloneAgent);
    }

    //create obstacles on terrain and store their reflex vertices
    private void InstantiateObstacles(int nbObstacles)
    {
        for (int i = 0; i < nbObstacles; i++)
        {
            GameObject obstacleL = Resources.Load("Prefabs/Obstacle") as GameObject;
            //random size but still Lshaped
            GameObject verti = obstacleL.transform.GetChild(0).GetChild(0).gameObject;   //get VertiL
            GameObject horiz = verti.transform.GetChild(0).gameObject; //get HorizL

            bool notOverlapping = false;

            while (!notOverlapping )
            {
                //random size
                float randSignSize = (float) Math.Round(UnityEngine.Random.value*2-1, MidpointRounding.AwayFromZero);
                float randSize = UnityEngine.Random.value * 0.5f;   //TODO improve size random shape
                horiz.transform.localScale += new Vector3(randSize, 0, 0);
                verti.transform.localScale += new Vector3(0, 0, randSignSize*randSize);

                //random position but doesn't overlap boundary 
                Transform ground = terrain.transform.GetChild(0);
                float maxSize = Mathf.Max(Mathf.Max(horiz.transform.localScale.x, verti.transform.localScale.x), Mathf.Max(horiz.transform.localScale.z, verti.transform.localScale.z));
                float randomPosX = (UnityEngine.Random.value * 2 - 1);
                float randomPosZ = (UnityEngine.Random.value * 2 - 1); 
                Vector3 randomPosOnGround = new Vector3(randomPosX * (ground.localScale.x / 2f -  (maxSize)),  //+ or - depends on sign of random
                                            ground.localScale.y,     //object need to be ON the ground 
                                            randomPosZ * (ground.localScale.z / 2f -  (maxSize)));
                Vector3 posOnGround = ground.position + randomPosOnGround;
                notOverlapping = true;
                //random rotation obstacle
                Quaternion randomRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);

                // check if no overlap with other obstacles
                Collider[] colliders = Physics.OverlapBox(posOnGround, obstacleL.transform.localScale, randomRotation);
                foreach (Collider col in colliders)
                {
                    if (col.CompareTag("Obstacle")) 
                    {
                        notOverlapping = false;
                    }
                }

                if (notOverlapping)
                {
                    GameObject obs = Instantiate(obstacleL, posOnGround, randomRotation, terrain.transform) as GameObject;
                    obstacles[i] = obs;
                }
            }

        }
    }


    private void createAgents( VisibilityGraph reducedVisibilityGraph)
    {
        for(int i = 0; i < nbAgents * 5; i++)   //init position availables
        {
            Agent.helperPositionAvailable.Add(i);   //help to know if agent is on ground or alcove
        }
        System.Random rnd = new System.Random();
        for (int i=0; i<nbAgents; i++)
        {
            Color c = new Color( (float)rnd.Next(256)%255/255, (float)rnd.Next(256)%255 /255, (float)(rnd.Next(256))%(255)/255, 1f);
            Agent agent = new Agent(terrain, c, reducedVisibilityGraph);
            agents.Add(agent);
        }
        //reset positions available
        Agent.helperPositionAvailable.Clear();
        for (int i = 0; i < nbAgents * 5; i++)   
        {
            Agent.helperPositionAvailable.Add(i);   
        }

    }
}
