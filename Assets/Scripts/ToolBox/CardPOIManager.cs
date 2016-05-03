﻿// Copyright Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using HoloToolkit.Unity;
using UnityEngine;

public class CardPOIManager : Singleton<CardPOIManager>
{
    [Header("Galaxy Card POI Fading")]
    [Tooltip("The time it takes for all points of interest to completely fade out when a card point of interest is selected.")]
    public float POIFadeOutTime = 1.0f;
    [Tooltip("How the opacity changes when all points of interest fade out when a card is selected.")]
    public AnimationCurve POIOpacityCurve;

    [Header("Galaxy Card Text Sliding")]
    [Tooltip("The time it takes for the card description to move from its unselected position to its selected position.")]
    public float DescriptionSlideOutTime = 1.0f;
    [Tooltip("The time it takes for the card description to move from its selected position to its unselected/highlight position.")]
    public float DescriptionSlideInTime = 0.5f;
    [Tooltip("The vector (local space) that descripts where the description card moves to when selected.")]
    public Vector3 DescriptionSlideDirection;
    [Tooltip("How the description card moves when it slides to selected and unselected positions.")]
    public AnimationCurve DescriptionSlideCurve;

    [HideInInspector]
    public bool CanTapCard = true;

    private void Start()
    {
        if (ViewLoader.Instance == null)
        {
            Destroy(this);
            Debug.LogError("No ViewLoader: Destroying CardPOIManager");
            return;
        }

        if (InputRouter.Instance == null)
        {
            Debug.LogWarning("CardPOIManager: No InputRouter was found, so cards cannot be cancelled by clicking anywhere.");
        }

        InputRouter.Instance.InputTapped += InputTapped;
    }

    private void OnDestroy()
    {
        if (InputRouter.Instance != null)
        {
            InputRouter.Instance.InputTapped -= InputTapped;
        }
    }

    private void InputTapped(UnityEngine.VR.WSA.Input.InteractionSourceKind arg1, int tapCount, Ray arg2)
    {
        HideAllCards();
    }

    public void HideAllCards()
    {
        GameObject currentContent = ViewLoader.Instance.GetCurrentContent();

        if (currentContent)
        {
            CardPointOfInterest[] cardPointOfInterests = currentContent.GetComponentsInChildren<CardPointOfInterest>(true);

            foreach (CardPointOfInterest cardPointOfInterest in cardPointOfInterests)
            {
                cardPointOfInterest.HideCard();
            }
        }
    }
}
