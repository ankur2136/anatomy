// Copyright Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using HoloToolkit.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : Singleton<TransitionManager>
{
    public enum FadeType
    {
        FadeIn,
        FadeOut,
        FadeUnload
    }

    public enum SpeedType
    {
        SpeedUp,
        SlowDown
    }

    [Serializable]
    public struct AudioTransition
    {
        public AudioClip StaticClip;
        public AudioClip MovingClip;

        public AudioTransition(AudioClip staticClip, AudioClip movingClip)
        {
            StaticClip = staticClip;
            MovingClip = movingClip;
        }
    }

    private bool isIntro = true;

    public bool IsIntro
    {
        get
        {
            return isIntro;
        }

        set
        {
            if (isIntro != value)
            {
                isIntro = value;

                if (isIntro == false && preLoadedContent != null)
                {
                    Destroy(preLoadedContent);
                }
            }
        }
    }

    public GameObject ViewVolume;
    public GameObject Tools;
    public GameObject Cube;
    private HoloCube holoCube;

    [Header("To Fullscreen Transition")]
    [Tooltip("The time it takes to complete a transition content from 'tesseract' to 'full screen' view.")]
    public float TransitionTimeFullscreen = 3.0f;
    [Tooltip("The opacity animation used to hide the box and UI controls when transitioning from the cube to fullscreen.")]
    public AnimationCurve OpacityCurveFullscreen;
    [Tooltip("The curve that defines how content transitions to fullscreen.")]
    public AnimationCurve TransitionCurveFullscreen;

    [Header("To Cube Transition")]
    [Tooltip("The time it takes to complete a transition content from 'full screen' to 'tesseract' view.")]
    public float TransitionTimeCube = 3.0f;
    [Tooltip("The opacity animation used to make the box and UI controls visible when transition from fullscreen to the cube.")]
    public AnimationCurve OpacityCurveCube;
    [Tooltip("The curve that defines how content transitions to the cube from fullscreen.")]
    public AnimationCurve TransitionCurveCube;

    [Header("Skybox Fade Out")]
    [Tooltip("The time it takes for a skybox to fade out.")]
    public float TransitionTimeSkyboxFadeOut = 0.5f;
    [Tooltip("The opacity animation from completely visible to hidden that happens as soon as a transition starts.")]
    public AnimationCurve OpacityCurveSkyboxFadeOut;

    [Header("Scene Transitions")]
    [Tooltip("The first time the galaxy appears, this defines how the scene moves into position.")]
    public AnimationCurve IntroTransitionCurveContentChange;
    [Tooltip("The curve that defines how content moves when transitioning from the galaxy to the solar system scene.")]
    public AnimationCurve GalaxyToSSTransitionCurveContentChange;
    [Tooltip("The curve that defines how content moves when transitioning from the solar system to the galaxy.")]
    public AnimationCurve SSToGalaxyTransitionCurveContentChange;
    [Tooltip("The curve that defines how content moves when transitioning from the solar system to a planet or the sun.")]
    public AnimationCurve SSToPlanetTransitionCurveContentChange;
    [Tooltip("The curve that defines how content moves (position and scale only) when transitioning from a planet or the sun to the solar system.")]
    public AnimationCurve PlanetToSSPositionScaleCurveContentChange;
    [Tooltip("The curve that defines how content moves (rotation only) when transitioning from a planet or the sun to the solar system.")]
    public AnimationCurve PlanetToSSRotationCurveContentChange;

    [Header("FirstScene")]
    [Tooltip("When the galaxy first loads, this controls the opacity of the galaxy (uses TransitionTimeOpeningScene for timing).")]
    public AnimationCurve OpacityCurveFirstScene;

    [Header("OpeningScene")]
    [Tooltip("The time it takes to fully transition from one scene opening and getting into position at the center of the cube or room.")]
    public float TransitionTimeOpeningScene = 3.0f;
    [Tooltip("Drives the opacity of the new scene that was loaded in when transitioning backwards.")]
    public AnimationCurve BackTransitionOpacityCurveContentChange;
    [Tooltip("Drives the opacity of the new scene that was loaded when transitioning from planet to solar system view.")]
    public AnimationCurve PlanetToSSTransitionOpacityCurveContentChange;

    [Header("Closing Scene")]
    [Tooltip("How long it takes to completely fade the galaxy scene when transitioning from this scene.")]
    public float GalaxyVisibilityTimeClosingScene = 1.0f;
    [Tooltip("How long it takes to completely fade the solar system scene when transitioning from this scene.")]
    public float SolarSystemVisibilityTimeClosingScene = 1.0f;
    [Tooltip("How long it takes to completely fade a planet or sun scene when transitioning from this scene.")]
    public float PlanetVisibilityTimeClosingScene = 1.0f;
    [Tooltip("Drives the opacity animation for the scene that is closing.")]
    public AnimationCurve OpacityCurveClosingScene;

    [Header("Start Transition")]
    public float StartTransitionTime = 1.0f;
    [Tooltip("Drives the POI opacity animation for the closing scene before content is loaded and starts moving into position.")]
    public AnimationCurve POIOpacityCurveStartTransition;
    public AnimationCurve OrbitSpeedCurveStartTransition;

    [Header("End Transition")]
    [Tooltip("This offset is applied to the time it takes to completely transition, so the end transition can start slightly before content has completely moved into place.")]
    public float EndTransitionTimeOffset = -1.0f;
    [Tooltip("The time it takes for one point of interest to completely fade out and the end of a transition.")]
    public float POIOpacityChangeTimeEndTransition = 1.0f;
    [Tooltip("The time between the previous and next points of interest fading out at the end of a transition.")]
    public float POIOpacityTimeOffsetEndTransition = 0.5f;
    [Tooltip("Drives the POI opacity animation for the opening scene after it has completely moved into place.")]
    public AnimationCurve POIOpacityCurveEndTransition;

    [Header("Skybox Fade In")]
    [Tooltip("The time it takes for the skybox to fade in. The skybox fades in at the end of a Content Change Transition.")]
    public float TransitionTimeSkyboxFadeIn = 0.5f;
    [Tooltip("The opacity animation from hidden to completely visible that happens at the end of a scene transition.")]
    public AnimationCurve OpacityCurveSkyboxFadeIn;

    [Header("Audio Transitions")]
    public AudioTransition GalaxyClips;
    public AudioTransition SolarSystemClips;
    public AudioTransition PlanetClips;
    public AudioTransition BackClips;

    public event Action ContentLoaded;

    public event Action ResetStarted;

    public event Action ResetFinished;

    // tracking data
    private GameObject doNotDisableScene;   // set when a scene transitions to prevent the scene from deactivating because that stops coroutines from running on it
    private GameObject prevSceneLoaded;     // tracks the last scene loaded for transitions when loading new scenes
    private GameObject loadSource;          // when new content is loaded, this is what the viewer selected to bring in that content
    private GameObject preLoadedContent;

    private string sceneToUnload;

    private bool inTransition = false;

    public bool InTransition
    {
        get
        {
            return inTransition;
        }
    }

    private bool fadingPointsOfInterest = false; // prevent content from physically transitioning until POIs have completely faded out

    private void Start()
    {
        if (ViewVolume == null)
        {
            Debug.LogError("TransitionManager: No view volume was specified for the cube view - unable to process transitions.");
            Destroy(this);
            return;
        }

        if (ViewLoader.Instance == null)
        {
            Debug.LogError("TransitionManager: No ViewLoader found - unable to process transitions.");
            Destroy(this);
            return;
        }

        holoCube = ViewVolume.GetComponentInChildren<HoloCube>();
        if (holoCube == null)
        {
            Debug.LogWarning("TransitionManager: No HoloCube found from the ViewVolume - unable to fade in/out the cube skybox.");
        }
    }

    public void ShowToolsAndCursor()
    {
        Tools.SetActive(true);
        Cursor.Instance.visible = true;
    }

    public void ResetView()
    {
        if (inTransition)
        {
            Debug.LogWarning("TransitionManager: Currently in a transition and cannot change view mode for '" + prevSceneLoaded.scene.name + "' until current transition completes.");
            return;
        }

        if (ResetStarted != null)
        {
            ResetStarted();
        }

        inTransition = true;

        Vector3 desiredPosition;
        Quaternion desiredRotation;
        float desiredScale;
        GetOrientationFromView(out desiredPosition, out desiredRotation, out desiredScale);

        StartCoroutine(TransitionContent(
            prevSceneLoaded,
            desiredPosition,
            desiredRotation,
            desiredScale,
            TransitionTimeCube,
            TransitionCurveCube,
            null, // no rotation
            TransitionCurveCube));

        RotateContentTowardViewer();

        CrossFadeToolbar(TransitionTimeCube);

        TriggerResetFinished(TransitionTimeCube);
    }

    /// <summary>
    /// Will hide to the toolbar for at least transitionTime and then shows it
    /// </summary>
    /// <param name="transitionTime"></param>
    private void CrossFadeToolbar(float transitionTime)
    {
        StartCoroutine(CrossFadeToolbarAsync(transitionTime));
    }

    private void TriggerResetFinished(float transitionTime)
    {
        StartCoroutine(TriggerResetFinishedAsync(transitionTime));
    }

    private IEnumerator CrossFadeToolbarAsync(float transitionTime)
    {
        var startTime = Time.time;
        yield return StartCoroutine(ToolManager.Instance.HideToolsAsync(instant: false));

        var timeLeftToWait = Mathf.Max(0, (Time.time - startTime) - transitionTime);

        if (timeLeftToWait > 0)
        {
            while (timeLeftToWait > 0)
            {
                timeLeftToWait -= Time.deltaTime;
                yield return null;
            }
        }

        yield return StartCoroutine(ToolManager.Instance.ShowToolsAsync());
    }

    private IEnumerator TriggerResetFinishedAsync(float transitionTime)
    {
        yield return new WaitForSeconds(transitionTime);

        if (ResetFinished != null)
        {
            ResetFinished();
        }
    }

    private void RotateContentTowardViewer()
    {
        var contentToCamera = Camera.main.transform.position - ViewLoader.Instance.transform.position;
        contentToCamera.y = 0;

        if (contentToCamera.magnitude <= float.Epsilon)
        {
            // We can't normalize that
            return;
        }

        contentToCamera.Normalize();
        var desiredRotation = Quaternion.LookRotation(contentToCamera);

        // Rotating the ViewLoader (box) to face the viewer
        StartCoroutine(TransitionContent(
            ViewLoader.Instance.gameObject,
            Vector3.zero,
            desiredRotation,
            1,
            TransitionTimeCube,
            null,
            TransitionCurveCube, // only rotation
            null));

        // Reset local tilt by rotating the content inside of the box to face the viewer
        StartCoroutine(TransitionContent(
           prevSceneLoaded,
           Vector3.zero,
           Quaternion.identity,
           1,
           TransitionTimeCube,
           null,
           TransitionCurveCube, // only rotation
           null,
           animateInLocalSpace: true));
    }

    private void StartTransitionForNewScene(GameObject source)
    {
        loadSource = source;

        // make sure nothing is fading or moving when we start a transition
        StopAllCoroutines();

        // fade in cube
        if (!ViewVolume.activeInHierarchy)
        {
            ViewVolume.SetActive(true);

            if (!IsIntro)
            {
                Tools.SetActive(true);
            }

            StartCoroutine(FadeContent(
                Cube,
                FadeType.FadeIn,
                TransitionTimeOpeningScene,
                OpacityCurveClosingScene));
        }

        if (prevSceneLoaded != null && !IsIntro)
        {
            // fade out points of interest for the current scene
            PointOfInterest[] focusPoints = prevSceneLoaded.GetComponentsInChildren<PointOfInterest>();
            foreach (PointOfInterest focalPoint in focusPoints)
            {
                // if faders has their coroutines killed, then need to be initialized to a disabled state
                Fader focalPointFader = focalPoint.GetComponent<Fader>();
                if (focalPointFader != null)
                {
                    focalPointFader.DisableFade();
                }

                StartCoroutine(FadeContent(
                    focalPoint.gameObject,
                    FadeType.FadeOut,
                    StartTransitionTime,
                    POIOpacityCurveStartTransition));
            }

            // slow down the solar system for the current scene
            StartCoroutine(TransitionOrbitSpeed(
                prevSceneLoaded,
                SpeedType.SlowDown,
                StartTransitionTime,
                OrbitSpeedCurveStartTransition));
            
            // this prevents content from starting transitions until points of interest have completely faded out
            fadingPointsOfInterest = focusPoints.Length > 0;
        }
    }

    public void InitializeLoadedContent(GameObject content)
    {

    }

    public void PreLoadScene(string sceneName)
    {
        inTransition = true;

        ViewLoader.Instance.LoadViewAsync(sceneName, false, PreLoadSceneLoaded);
    }

    private void PreLoadSceneLoaded(GameObject content, string oldSceneName)
    {
    }

    public void LoadPrevScene(string sceneName)
    {
        if (inTransition)
        {
            Debug.LogWarning("TransitionManager: Currently in a transition and cannot change view to '" + sceneName + "' until current transition completes.");
            return;
        }

        inTransition = true;
        StartTransitionForNewScene(null);

        SwitchAudioClips(sceneName, forwardNavigation: false);
        ViewLoader.Instance.LoadViewAsync(sceneName, false, PrevSceneLoaded);
    }

    private void PrevSceneLoaded(GameObject content, string oldSceneName)
    {
        sceneToUnload = oldSceneName;

        StartCoroutine(PrevSceneLoadedCoroutine(content));
    }

    private IEnumerator PrevSceneLoadedCoroutine(GameObject content)
    {
         yield return new WaitForFixedUpdate();

    }

    public void LoadNextScene(string sceneName, GameObject sourceObject)
    {
        if (inTransition)
        {
            Debug.LogWarning("TransitionManager: Currently in a transition and cannot change view to '" + sceneName + "' until current transition completes.");
            return;
        }

        inTransition = true;
        StartTransitionForNewScene(sourceObject);

        SwitchAudioClips(sceneName);
        ViewLoader.Instance.LoadViewAsync(sceneName, true, NextSceneLoaded);
    }

    private void NextSceneLoaded(GameObject content, string oldSceneName)
    {
        sceneToUnload = oldSceneName;

        StartCoroutine(NextSceneLoadedCoroutine(content));
    }

    private IEnumerator NextSceneLoadedCoroutine(GameObject content)
    {
        yield return new WaitForEndOfFrame();
    }

    private void GetOrientationFromView(out Vector3 position, out Quaternion rotation, out float scale)
    {
        position = ViewLoader.Instance.transform.position;
        rotation = ViewLoader.Instance.transform.rotation;
        scale = Mathf.Max(ViewVolume.transform.lossyScale.x, ViewVolume.transform.lossyScale.y, ViewVolume.transform.lossyScale.z);
    }

    private AnimationCurve GetContentRotationCurve(string loadedSceneName)
    {
        if (prevSceneLoaded == null)
        {
            return IntroTransitionCurveContentChange;
        }

        if (loadedSceneName.Contains("GalaxyView"))
        {
            return SSToGalaxyTransitionCurveContentChange;
        }

        if (loadedSceneName.Contains("SolarSystemView"))
        {
            if (prevSceneLoaded.name.Contains("GalaxyView"))
            {
                return GalaxyToSSTransitionCurveContentChange;
            }
            else
            {
                return PlanetToSSRotationCurveContentChange;
            }
        }

        return SSToPlanetTransitionCurveContentChange;
    }

    private AnimationCurve GetContentTransitionCurve(string loadedSceneName)
    {
        if (prevSceneLoaded == null)
        {
            return IntroTransitionCurveContentChange;
        }

        if (loadedSceneName.Contains("GalaxyView"))
        {
            return SSToGalaxyTransitionCurveContentChange;
        }

        if (loadedSceneName.Contains("SolarSystemView"))
        {
            if (prevSceneLoaded.name.Contains("GalaxyView"))
            {
                return GalaxyToSSTransitionCurveContentChange;
            }
            else
            {
                return PlanetToSSPositionScaleCurveContentChange;
            }
        }

        return SSToPlanetTransitionCurveContentChange;
    }

    private float GetClosingSceneVisibilityTime()
    {
        if (prevSceneLoaded == null)
        {
            Debug.LogError("TransitionManager: Unable to find the time it takes to fade the last loaded scene because no previous loaded scene was found.");
            return 0.0f;
        }

        if (prevSceneLoaded.name.Contains("GalaxyView"))
        {
            return GalaxyVisibilityTimeClosingScene;
        }

        if (prevSceneLoaded.name.Contains("SolarSystemView"))
        {
            return SolarSystemVisibilityTimeClosingScene;
        }

        return PlanetVisibilityTimeClosingScene;
    }

    public IEnumerator FadeContent(GameObject content, FadeType fadeType, float fadeDuration, AnimationCurve opacityCurve, float fadeTimeOffset = 0.0f)
    {
        yield return new WaitForEndOfFrame();
    }

    private IEnumerator TransitionContent(GameObject content, Vector3 targetPosition, Quaternion targetRotation, float targetSize,
        float transitionDuration, AnimationCurve positionCurve, AnimationCurve rotationCurve, AnimationCurve scaleCurve, float transitionTimeOffset = 0.0f, bool animateInLocalSpace = false)
    {
        // Disable all colliders during a transition to workaround physics issues with scaling box and mesh colliders down to really small numbers (~0)
        Collider[] colliders = content.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        // Prevent content from changing until the points of interest have completely faded out
        while (fadingPointsOfInterest)
        {
            yield return null;
        }

        SceneSizer contentSizer = content.GetComponent<SceneSizer>();
        Vector3 startPosition = animateInLocalSpace ? content.transform.localPosition : content.transform.position;
        Quaternion startRotation = animateInLocalSpace ? content.transform.localRotation : content.transform.rotation;
        Vector3 startScale = content.transform.localScale;

        if (contentSizer != null)
        {
            targetSize = contentSizer.GetScalar(targetSize);
        }

        Vector3 finalScale = new Vector3(targetSize, targetSize, targetSize);

        float time = -transitionTimeOffset;
        float timeFraction = 0.0f;
        do
        {
            time += Time.deltaTime;
            timeFraction = Mathf.Clamp01(time / transitionDuration);

            if (positionCurve != null)
            {
                if (animateInLocalSpace)
                {
                    content.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, Mathf.Clamp01(positionCurve.Evaluate(timeFraction)));
                }
                else
                {
                    content.transform.position = Vector3.Lerp(startPosition, targetPosition, Mathf.Clamp01(positionCurve.Evaluate(timeFraction)));
                }
            }

            if (rotationCurve != null)
            {
                if (animateInLocalSpace)
                {
                    content.transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, Mathf.Clamp01(rotationCurve.Evaluate(timeFraction)));
                }
                else
                {
                    content.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, Mathf.Clamp01(rotationCurve.Evaluate(timeFraction)));
                }
            }

            if (scaleCurve != null)
            {
                content.transform.localScale = Vector3.Lerp(startScale, finalScale, Mathf.Clamp01(scaleCurve.Evaluate(timeFraction)));
            }

            yield return null;
        }
        while (timeFraction < 1.0f && content != null);

        if (content == null)
        {
            yield break;
        }

        if (ContentLoaded != null && content == prevSceneLoaded)
        {
            ContentLoaded();
        }

        inTransition = false;

        // Reenable colliders at the end of the transition
        foreach (Collider collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

        ToolManager.Instance.ShowTools();
    }

    private IEnumerator TransitionOrbitSpeed(GameObject content, SpeedType speedType, float duration, AnimationCurve speedCurve, float transitionTimeOffset = 0.0f)
    {
        OrbitUpdater[] orbits = content.GetComponentsInChildren<OrbitUpdater>();

        if (orbits.Length > 0)
        {
            // If the orbit is speeding up, start with zeroed speed
            if (speedType == SpeedType.SpeedUp)
            {
                foreach (OrbitUpdater orbit in orbits)
                {
                    orbit.TransitionSpeedMultiplier = 0.0f;
                }
            }

            // Prevent content from changing until the points of interest have completely faded out
            while (fadingPointsOfInterest)
            {
                yield return null;
            }

            Vector2 scalarRange = Vector2.one;

            switch (speedType)
            {
                case SpeedType.SpeedUp:
                    scalarRange.x = 0.0f;
                    break;
                case SpeedType.SlowDown:
                    scalarRange.y = 0.0f;
                    break;
            }

            float time = -transitionTimeOffset;
            float timeFraction = 0.0f;
            do
            {
                time += Time.deltaTime;
                timeFraction = Mathf.Clamp01(time / duration);

                foreach (OrbitUpdater orbit in orbits)
                {
                    orbit.TransitionSpeedMultiplier = speedCurve != null
                        ? Mathf.Lerp(scalarRange.x, scalarRange.y, Mathf.Clamp01(speedCurve.Evaluate(timeFraction)))
                        : timeFraction;
                }

                yield return null;
            }
            while (timeFraction < 1.0f && orbits != null);
        }
    }

    private void SwitchAudioClips(string sceneName, bool forwardNavigation = true)
    {
        AudioClip staticClip = null;
        AudioClip movingClip = null;

        if (!forwardNavigation)
        {
            staticClip = BackClips.StaticClip;
            movingClip = BackClips.MovingClip;
        }
        else if (sceneName == "SolarSystemView")
        {
            staticClip = SolarSystemClips.StaticClip;
            movingClip = SolarSystemClips.MovingClip;
        }
        else if (!IsIntro)
        {
            staticClip = PlanetClips.StaticClip;
            movingClip = PlanetClips.MovingClip;
        }

        ViewLoader.Instance.SetTransitionSFX(staticClip, movingClip);
    }
}
