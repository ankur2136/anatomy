// Copyright Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using HoloToolkit.Unity;
using System;
using UnityEngine;
using UnityEngine.VR.WSA.Input;

public class PlacementControl : GazeSelectionTarget
{
    public Animator[] indicators;

    public float TightTagalongDistance = 2.0f;

    private bool isHolding;

    public bool IsHolding
    {
        get { return isHolding; }
    }

    public event Action ContentHeld;

    public event Action ContentPlaced;

    private GameObject contentVolume;
    private TightTagalong volumeTightTagalong;
    private Interpolator volumeInterpolator;

    private void Start()
    {
        contentVolume = TransitionManager.Instance.ViewVolume;
        volumeTightTagalong = contentVolume.GetComponent<TightTagalong>();
        volumeInterpolator = contentVolume.GetComponent<Interpolator>();
    }

    public void TogglePinnedState()
    {
        if (!isHolding)
        {
            // Collider provides a way to prevent the content from being accessed
            // while placing the volume, since we air tap to place the volume
            foreach (var element in indicators)
            {
                element.SetBool("Selected", true);
            }

            // Enable TightTagalong, which enabled the interpolator by default
            volumeTightTagalong.enabled = true;
            volumeTightTagalong.distanceToHead = TightTagalongDistance;

            // Start moving content
            ViewLoader.Instance.transform.SetParent(contentVolume.transform, true);

            if (Cursor.Instance)
            {
                Cursor.Instance.ApplyCursorState(CursorState.Pin);
            }

            if (ContentHeld != null)
            {
                ContentHeld();
            }

            isHolding = true;
        }
        else
        {
            ReleaseContent();
        }
    }

    public override bool OnTapped(InteractionSourceKind source, int tapCount, Ray ray)
    {
        if (isHolding)
        {
            ReleaseContent();
            return true;
        }

        return false;
    }

    private void ReleaseContent()
    {
        // Disable the placement volume collider so that our air taps can
        // target the content inside the volume
        foreach (var element in indicators)
        {
            element.SetBool("Selected", false);
        }

        // Disable TightTagalong and interpolator
        volumeTightTagalong.enabled = false;
        volumeInterpolator.enabled = false;

        // Stop moving content
        ViewLoader.Instance.transform.SetParent(null, true);

        ToolManager.Instance.UnlockTools();

        //// TODO: Play sound for placement

        Cursor.Instance.ClearToolState();

        isHolding = false;

        if (ContentPlaced != null)
        {
            ContentPlaced();
        }
    }
}
