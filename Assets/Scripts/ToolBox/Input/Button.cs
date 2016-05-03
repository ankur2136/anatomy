﻿// Copyright Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using UnityEngine;
using UnityEngine.VR.WSA.Input;
using System;

public enum ButtonType
{
    Reset,
    MoveCube,
    Back,
    Hide,
    Show,
    About
}

public class Button : GazeSelectionTarget, IFadeTarget
{
    public GameObject TooltipObject;
    public Material DefaultMaterial;
    public Material HightlightMaterial;
    public ButtonType type;
    public bool IsDisabled;

    public event Action<ButtonType> ButtonPressed;

    public static bool HasGaze
    {
        get
        {
            return hasGaze;
        }
    }

    private float currentOpacity = 1;

    public float Opacity
    {
        get
        {
            return currentOpacity;
        }

        set
        {
            currentOpacity = value;

            ApplyOpacity(DefaultMaterial, value);
            ApplyOpacity(HightlightMaterial, value);
        }
    }

    private static bool hasGaze = false;

    private MeshRenderer meshRenderer;
    private PlacementControl cubeToMove;

    private void ApplyOpacity(Material material, float value)
    {
        value = Mathf.Clamp01(value);

        if (material)
        {
            material.SetFloat("_TransitionAlpha", value);
            material.SetInt("_SRCBLEND", value < 1 ? (int)UnityEngine.Rendering.BlendMode.SrcAlpha : (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DSTBLEND", value < 1 ? (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWRITE", value < 1 ? 0 : 1);
        }
    }

    private void Awake()
    {
        if (DefaultMaterial == null)
        {
            Debug.LogWarning(gameObject.name + " Button has no default material.");
        }

        if (HightlightMaterial == null)
        {
            Debug.LogWarning(gameObject.name + " Button has no highlight material.");
        }

        meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (meshRenderer == null)
        {
            Debug.LogWarning(gameObject.name + " Button has no renderer.");
        }
    }

    private void Start()
    {
        cubeToMove = TransitionManager.Instance.ViewVolume.GetComponentInChildren<PlacementControl>();

        if (TooltipObject != null)
        {
            TooltipObject.SetActive(false);
        }
    }

    public void Highlight()
    {
        if (!ToolManager.Instance.IsLocked)
        {
            ToolSounds.Instance.PlayHighlightSound();
            meshRenderer.material = HightlightMaterial;

            if (TooltipObject != null)
            {
                TooltipObject.SetActive(true);
            }
        }
    }

    public void RemoveHighlight()
    {
        if (!ToolManager.Instance.IsLocked)
        {
            ToolSounds.Instance.PlayRemoveHighlightSound();
            meshRenderer.material = DefaultMaterial;

            if (TooltipObject != null)
            {
                TooltipObject.SetActive(false);
            }
        }
    }

    public override void OnGazeSelect()
    {
        hasGaze = true;
        Highlight();
    }

    public override void OnGazeDeselect()
    {
        hasGaze = false;
        RemoveHighlight();
    }

    public override bool OnTapped(InteractionSourceKind source, int tapCount, Ray ray)
    {
        if (ToolManager.Instance.IsLocked)
        {
            return false;
        }

        bool didAction = false;
        if (IsDisabled)
        {
            ToolSounds.Instance.PlayDisabledClickSound();
        }
        else
        {
            ToolSounds.Instance.PlayClickSound();
            ButtonAction();
            didAction = true;
        }

        return didAction;
    }

    protected override void VoiceCommandCallback(string command)
    {
        if (!TransitionManager.Instance.InTransition)
        {
            ButtonAction();
        }
    }

    [ContextMenu("ButtonAction")]
    public void ButtonAction()
    {
        // do not have two buttons active at the same time
        if (ToolManager.Instance != null && ToolManager.Instance.SelectedTool != null)
        {
            ToolManager.Instance.SelectedTool.Unselect();
        }

        if (ButtonPressed != null)
        {
            ButtonPressed(type);
        }

        switch (type)
        {
            case ButtonType.Back:
                if (cubeToMove == null || !cubeToMove.IsHolding)
                {
                    ViewLoader.Instance.GoBack();
                }

                break;

            case ButtonType.Reset:
                if (cubeToMove == null || !cubeToMove.IsHolding)
                {
                    if (TransitionManager.Instance)
                    {
                        TransitionManager.Instance.ResetView();
                    }
                }

                break;

            case ButtonType.MoveCube:
                ToolManager.Instance.LockTools();
                cubeToMove.TogglePinnedState();
                break;

            case ButtonType.Show:
            case ButtonType.Hide:
                ToolManager.Instance.ToggleTools();
                break;
        }
    }
}
