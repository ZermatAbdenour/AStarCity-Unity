using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class CarControlller : MonoBehaviour
{
    public float UpdatePathRate = 10;
    public AStarAlgorithmController.node StartNode;
    public AStarAlgorithmController.node EndNode;
    private AStarAlgorithmController.ExploredNode[] Path;
    public Vector3[] PathPoints;
    private int currentPointIndex = 0;
    private int currentLinkPointIndex = 0;

    private bool StartSelected = false;
    private bool EndSelected = false;
    [Header("Path")]
    public TMPro.TextMeshProUGUI DistanceText;
    public GameObject StartIndicator;
    public GameObject EndIndicator;
    public GameObject ClosestNode;


    [Header("Movement")]
    public float Speed;
    public float CurrentLerpValue;


    public void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100))
        {
            AStarAlgorithmController.node nearestNode = AStarAlgorithmController.Instance.getNearestNode(hit.point);
            Vector3 ClosestNodePoint = AStarAlgorithmController.Instance.GetNodePositionById(nearestNode.id);

            bool PathChanged = false;
            ClosestNode.transform.position = ClosestNodePoint;
            if (Input.GetKeyDown(KeyCode.Alpha1)) {
                StartIndicator.SetActive(true);
                StartSelected = true;
                StartIndicator.transform.position = ClosestNodePoint;
                StartNode = nearestNode;
                transform.position = ClosestNodePoint;
                PathPoints = new Vector3[0];
                Path = new AStarAlgorithmController.ExploredNode[0];
                PathChanged = true;
            }
            if(Input.GetKeyDown(KeyCode.Alpha2))
            {
                EndIndicator.SetActive(true);
                EndSelected = true;
                EndIndicator.transform.position = ClosestNodePoint;
                EndNode = nearestNode;
                PathChanged = true;
            }

            if (PathChanged && StartSelected&& EndSelected)
            {
                Path = AStarAlgorithmController.Instance.CaculateShortestPath(StartNode, EndNode);
                DistanceText.text = "Distance : " + ((int)Path[Path.Length - 1].GCost).ToString() + " m";
            }
        }

        if (Input.GetKeyDown(KeyCode.N) && StartSelected && EndSelected)
        {
            PathPoints = AStarAlgorithmController.Instance.GetTotalPathPoints(Path);
            transform.position = PathPoints[0];
            currentPointIndex = 0;
        }


        FollowPath();
    }


    public void FollowPath()
    {
        if (PathPoints.Length == 0)
            return;
        Vector3 nextPoint = PathPoints[currentPointIndex];

        // Calculate distance between current position and target position
        float distanceToTarget = Vector3.Distance(transform.position, nextPoint);

        // If the cube is close enough to the target point, move to the next point
        if (distanceToTarget < 0.1f && currentPointIndex< PathPoints.Length-1)
        {
            currentPointIndex = currentPointIndex + 1;
        }

        if (distanceToTarget > 0.1f && currentPointIndex == PathPoints.Length - 1 || currentPointIndex != PathPoints.Length - 1)
        {
            transform.forward = (nextPoint - transform.position).normalized;
            transform.Translate(Vector3.forward * Speed * Time.deltaTime);
        }      
    }

}
