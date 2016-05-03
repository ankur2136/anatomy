﻿using UnityEngine;

/// <summary>
/// The Interactible class flags a Game Object as being "Interactible".
/// Determines what happens when an Interactible is being gazed at.
/// </summary>
public class Interactible : MonoBehaviour
{
    [Tooltip("Audio clip to play when interacting with this hologram.")]
    public AudioClip TargetFeedbackSound;
    private AudioSource audioSource;

    private Material[] defaultMaterials;
    private Material[] materialsWithHighlight;
    private Material highlightMaterial;

    void Start()
    {
        defaultMaterials = GetComponent<Renderer>().materials;

        highlightMaterial = Resources.Load("Materials/AdditiveRimShader", typeof(Material)) as Material;

        // Add highlightMaterial to materialsWithHighlight.
        materialsWithHighlight = new Material[defaultMaterials.Length + 1];
        for (int i = 0; i < defaultMaterials.Length; i++)
        {
            materialsWithHighlight[i] = defaultMaterials[i];
        }
        materialsWithHighlight[materialsWithHighlight.Length - 1] = highlightMaterial;

        // Add a BoxCollider if the interactible does not contain one.
        Collider collider = GetComponentInChildren<Collider>();
        if (collider == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }

        EnableAudioHapticFeedback();
    }

    private void EnableAudioHapticFeedback()
    {
        // If this hologram has an audio clip, add an AudioSource with this clip.
        if (TargetFeedbackSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.clip = TargetFeedbackSound;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1;
            audioSource.dopplerLevel = 0;
        }
    }

    void LateUpdate()
    {
        Debug.ClearDeveloperConsole();
    }

    /* TODO: DEVELOPER CODING EXERCISE 2.d */

    void GazeEntered()
    {
        if (materialsWithHighlight != null && highlightMaterial != null)
        {
            // 2.d: Set GetComponent Renderer's materials to
            // materialsWithHighlight when gazed at.
            GetComponent<Renderer>().materials = materialsWithHighlight;
        }
    }

    void GazeExited()
    {
        if (defaultMaterials != null && highlightMaterial != null)
        {
            // 2.d: Set GetComponent Renderer's materials to 
            // defaultMaterials when gazed away.
            GetComponent<Renderer>().materials = defaultMaterials;
        }
    }

    void OnSelect()
    {
        // Play the audioSource haptic feedback when we gaze and select a hologram.
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }

        /* TODO: DEVELOPER CODING EXERCISE 6.a */
        // 6.a: Handle the OnSelect by sending a PerformTagAlong message.
        this.SendMessage("PerformTagAlong");
    }
}