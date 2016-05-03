﻿// Copyright Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System.Collections.Generic;
using UnityEngine;

public class GazeSelection : MonoBehaviour
{
    [HeaderAttribute("Gaze Search")]
    [Tooltip("Distance along the gaze vector to search for valid targets to select.")]
    public float GazeDistance = 30.0f;
    [HeaderAttribute("Spherical Cone Search")]
    public bool UseSphericalConeSearch = true;
    [Tooltip("If no objects are found along the gaze vector, the average position of objects found within this angle of the gaze vector are selected.")]
    public float GazeSpreadDegrees = 30.0f;

    // ordered from closest to gaze to farthest
    private SortedList<float, RaycastHit> selectedTargets;

    public IList<RaycastHit> SelectedTargets
    {
        get { return selectedTargets != null ? selectedTargets.Values : null; }
    }

    private float targetSpreadMinValue;
    private PlacementControl placementControl;
    
    private void Start()
    {
        if (Camera.main == null)
        {
            Debug.LogError(" GazeSelection:No main camera exists in the scene, unable to use GazeSelection.", this);
            GameObject.Destroy(this);
            return;
        }

        if (Cursor.Instance == null)
        {
            Debug.LogError("GazeSelection: no target layer masks can be used because the Cursor was not found.", this);
            GameObject.Destroy(this);
            return;
        }

        if (TransitionManager.Instance == null)
        {
            Debug.LogWarning("GazeSelection: No TransitionManager found, so input is not disabled during transitions.");
        }
        else if (TransitionManager.Instance.ViewVolume != null)
        {
            placementControl = TransitionManager.Instance.ViewVolume.GetComponentInChildren<PlacementControl>();
        }

        selectedTargets = new SortedList<float, RaycastHit>();
        targetSpreadMinValue = Mathf.Cos(Mathf.Deg2Rad * GazeSpreadDegrees);
    }

    public void Update()
    {
        selectedTargets.Clear();
        
        if ((TransitionManager.Instance == null || (!TransitionManager.Instance.InTransition && !TransitionManager.Instance.IsIntro)) &&     // in the middle of a scene transition or if it is the intro, prevent gaze selection
            (placementControl == null || !placementControl.IsHolding))                                                                       // the cube is being placed, prevent gaze selection
        {
            Vector3 gazeStart = Camera.main.transform.position + (Camera.main.nearClipPlane * Camera.main.transform.forward);

            foreach (Cursor.PriorityLayerMask priorityMask in Cursor.Instance.prioritizedCursorMask)
            {
                switch (priorityMask.collisionType)
                {
                    case Cursor.CursorCollisionSearch.RaycastSearch:
                        RaycastHit info;
                        if (Physics.Raycast(gazeStart, Camera.main.transform.forward, out info, GazeDistance, priorityMask.layers))
                        {
                            selectedTargets.Add(0.0f, info);
                        }

                        break;

                    case Cursor.CursorCollisionSearch.SphereCastSearch:
                        if (UseSphericalConeSearch)
                        {
                            // get all target objects in a sphere from the camera
                            RaycastHit[] hitTargets = Physics.SphereCastAll(gazeStart, GazeDistance, Camera.main.transform.forward, 0.0f, priorityMask.layers);

                            // only consider target objects that are within the target spread angle specified on start
                            foreach (RaycastHit target in hitTargets)
                            {
                                Vector3 toTarget = Vector3.Normalize(target.transform.position - Camera.main.transform.position);
                                float dotProduct = Vector3.Dot(Camera.main.transform.forward, toTarget);
                                if (Vector3.Dot(Camera.main.transform.forward, toTarget) >= targetSpreadMinValue)
                                {
                                    selectedTargets[-dotProduct] = target;
                                }
                            }
                        }

                        break;
                }

                if (selectedTargets.Count > 0)
                {
                    break;
                }
            }
        }
    }
}
