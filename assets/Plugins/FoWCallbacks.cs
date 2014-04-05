using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// Manages callbacks (Funcs/Actions) between FoWManager and user code.
/// See documentation for details on each of the callbacks below.
/// </summary>
public class FoWCallbacks : MonoBehaviour
{
    public Func<int, int, int> TestCall = null;

    public Func<int, int, int, int, bool> VisibilityTest = null;

    public Action<int, int> OnTileFirstVisible = null;
    public Action<int, int> OnTileBecomesVisible = null;
    public Action<int, int> OnTileBecomesExplored = null;
    public Action<int, int> OnTileBecomesHidden = null;

    public Action<GameObject> OnNonPlayerUnitBecomesVisible = null;
    public Action<GameObject> OnNonPlayerUnitBecomesExplored = null;
    public Action<GameObject> OnNonPlayerUnitBecomesHidden = null;
    public Func<GameObject, GameObject> OnAddGhost = null;
    public Action<GameObject, GameObject> OnRemoveGhost = null;

    private static FoWCallbacks _instance;
    public static FoWCallbacks FindInstance()
    {
        return _instance ?? (_instance = FindObjectOfType(typeof (FoWCallbacks)) as FoWCallbacks);
    }
}
