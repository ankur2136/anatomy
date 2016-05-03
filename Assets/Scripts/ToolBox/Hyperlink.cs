// Copyright Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using UnityEngine;
using UnityEngine.VR.WSA.Input;

public class Hyperlink : GazeSelectionTarget
{
    public string URL;

    public event Action Clicked;
    
    public override bool OnTapped(InteractionSourceKind source, int tapCount, Ray ray)
    {
        if (Clicked != null)
        {
            Clicked();
        }

        if (!string.IsNullOrEmpty(URL))
        {
#if NETFX_CORE
            var uri = new System.Uri(URL);
            var unused = Windows.System.Launcher.LaunchUriAsync(uri);
#else
            Application.OpenURL(URL);
#endif 
        }

        return true;
    }
}
