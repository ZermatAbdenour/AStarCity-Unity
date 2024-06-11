using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using JetBrains.Annotations;
using Unity.VisualScripting;
using System.Linq;

public class AStarAlgorithmController : MonoBehaviour
{
    public static AStarAlgorithmController Instance { get; private set; }
    public GraphData UsedGraphData;
    public Vector2 Center = new Vector2((4.9085f + 4.8968f) / 2, (36.6444f + 36.6354f) / 2);
    public Vector2 Transformer =new Vector2(12000f,12000f);
    public Transform Parent;
    public GameObject Prefab;

    [System.Serializable]
    public struct GraphData
    {
        public node[] nodes;
        public link[] links;
    }

    [System.Serializable]
    public struct node {
        public float y;
        public float x;
        public int street_count;
        public int id;
    }
    [System.Serializable]
    public struct link
    {
        public int osmid;
        public string Ref;
        public string name;
        public string highway;
        public string oneway;
        public string reversed;
        public float length;
        public Geometry geometry;
        [System.Serializable]
        public struct Geometry
        {
            public string type;
            public float[][] coordinates;

        }
        public int source;
        public int target;
    }

    public List<ExploredNode> ExpNodes;

    private void Awake()
    {
        Instance = this;
        string path = "Assets/Data/graph_data.json";
        string json = File.ReadAllText(path);
        UsedGraphData = JsonUtility.FromJson<GraphData>(json);
    }

    [System.Serializable]
    public class ExploredNode
    {
        public int nodeId;
        public int previousNodeId;
        public int linkid;//Here we save the index of the link because links does not have ids
        public float FCost { get { return GCost + HCost; } }
        public float GCost;
        public float HCost;
        public bool Expended;
    }
    public Vector3 ApplyTransformer(float x,float y)
    {
        return new Vector3((x - Center.x) * Transformer.x, 0, (y - Center.y) * Transformer.y);
    }


    public ExploredNode[] CaculateShortestPath(node startNode,node endNode)
    {
        ExpNodes = new List<ExploredNode>();

        //Add the Start Node to the Explored Node
        ExploredNode startExploredNode = new ExploredNode();
        startExploredNode.nodeId = startNode.id;
        startExploredNode.previousNodeId = startNode.id;
        startExploredNode.GCost = 0;
        startExploredNode.linkid = -1;
        startExploredNode.HCost = CalculateEuclideanDistance(startNode,endNode);

        ExpNodes.Add(startExploredNode);
        int Stop = 100;
        ExploredNode expendNode = startExploredNode;
        while (expendNode.nodeId != endNode.id && Stop >=0)
        {
            expendNode.Expended = true;
            List<link> links = GetConnectedLinks(expendNode.nodeId);

            for(int i = 0; i < links.Count; i++)
            {
                node linkednode = GetNodeById(links[i].target);
                //Check if the node already explored
                bool explored = false;
                foreach (ExploredNode exploredNode in ExpNodes)
                {
                    if(exploredNode.nodeId == linkednode.id)
                    {
                        explored = true;
                        break;
                    }
                }
                if (!explored)
                {
                    ExploredNode newExploredNode = new ExploredNode();
                    newExploredNode.nodeId = links[i].target;
                    newExploredNode.previousNodeId = expendNode.nodeId;
                    newExploredNode.linkid = i;
                    newExploredNode.GCost = expendNode.GCost + links[i].length;
                    newExploredNode.HCost = CalculateEuclideanDistance(linkednode, endNode);
                    ExpNodes.Add(newExploredNode);
                }
            }

            float SmallestFcost = Mathf.Infinity;
            ExploredNode nodetoExpend = new ExploredNode();
            foreach (ExploredNode expnode in ExpNodes)
            {
                if (!expnode.Expended && expnode.FCost < SmallestFcost)
                {
                    SmallestFcost = expnode.FCost;
                    nodetoExpend = expnode;
                }
            }
            expendNode = nodetoExpend;
            Stop--;
        }

        List<ExploredNode> shortestPath = new List<ExploredNode>();

        ExploredNode backNode = expendNode;
        shortestPath.Add(backNode);

        while(backNode != startExploredNode)
        {
            backNode = GetExploredNodeById(ExpNodes.ToArray(),backNode.previousNodeId);
            shortestPath.Add(backNode);
        }
        shortestPath.Reverse();


        return shortestPath.ToArray();
    }

    public List<link> GetConnectedLinks(int nodeid)
    {
        List<link> links = new List<link>(); 
        for(int i =0;i< UsedGraphData.links.Length;i++)
        {
            if(UsedGraphData.links[i].source == nodeid)
            {
                links.Add(UsedGraphData.links[i]);
            }
        }
        return links;
    }

    public float CalculateEuclideanDistance(node node1,node node2)
    {
        return Vector3.Distance(ApplyTransformer(node1.x, node1.y), ApplyTransformer(node2.x, node2.y));
    }
    
    public node GetNodeById(int id)
    {
        foreach(node node in UsedGraphData.nodes)
        {
            if(node.id == id)
                return node;
        }

        return new node();
    }

    public ExploredNode GetExploredNodeById(ExploredNode[] expnodes, int id)
    {
        foreach (ExploredNode node in expnodes)
        {
            if (node.nodeId == id)
                return node;
        }

        return new ExploredNode();
    }

    public node getNearestNode(Vector3 position)
    {
        float nearestNodeDistance = Mathf.Infinity;
        node nearestNode = new node();
        foreach (node node in UsedGraphData.nodes)
        {
            float dist = Vector3.Distance(position, ApplyTransformer(node.x, node.y));
            if (dist < nearestNodeDistance)
            {
                nearestNodeDistance = dist;
                nearestNode = node;
            }
        }

        return nearestNode;
    }

    public Vector3 GetNodePositionById(int id)
    {
        node node = GetNodeById(id);
        return ApplyTransformer(node.x, node.y);
    }

    public Vector3[] GetTotalPathPoints(ExploredNode[] ExploredNodes)
    {
        List<Vector3> PathPoints = new List<Vector3>();
        PathPoints.Add(GetNodePositionById(ExploredNodes[0].nodeId));

        for(int i  = 1;i< ExploredNodes.Length;i++)
        {
            link.Geometry geometry = UsedGraphData.links[ExploredNodes[i].linkid].geometry;
            print(geometry.coordinates);
            if(geometry.coordinates != null)
            {
                for (int j = 0; j < geometry.coordinates.Length; j++)
                {
                    Vector3 point = ApplyTransformer(geometry.coordinates[j][0], geometry.coordinates[j][1]);
                    PathPoints.Add(point);
                }
            }
            else
            {
                Vector3 point = GetNodePositionById(ExploredNodes[i].nodeId);
                PathPoints.Add(point);
            }


            
        }

        return PathPoints.ToArray();
    }
}
