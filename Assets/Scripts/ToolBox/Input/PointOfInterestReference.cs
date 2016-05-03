// Copyright Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using UnityEngine;
using UnityEngine.VR.WSA.Input;

public class PointOfInterestReference : GazeSelectionTarget
{
    public PointOfInterest pointOfInterest;

    private void Start()
    {
        if (pointOfInterest == null)
        {
            Debug.LogError("PointOfInterestReference: No point of interest is specified for '" + name + "' - removing component.");
            Destroy(this);
            return;
        }
    }

    public override void OnGazeSelect()
    {
        pointOfInterest.OnGazeSelect();
    }

    public override void OnGazeDeselect()
    {
        pointOfInterest.OnGazeDeselect();
    }

    public override bool OnTapped(InteractionSourceKind source, int tapCount, Ray ray)
    {
        pointOfInterest.OnTapped(source, tapCount, ray);
        return true;
    }
}
