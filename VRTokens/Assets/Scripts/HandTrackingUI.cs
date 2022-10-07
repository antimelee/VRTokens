using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandTrackingUI : MonoBehaviour
{
    // Start is called before the first frame update
    public OVRHand hand;
    public OVRInputModule inputModelue;

    private void Start()
    {
        Debug.Log("#####RayCastingUI Start");
        inputModelue.rayTransform = null;
    }

    /// <summary>
    /// When user pinch there right hand by index finger and thumb, 
    /// OnPinchSelect will be called
    /// </summary>
    /// <param name="layerIndex"></param>
    public void OnPinchSelect()
    {
        Debug.Log("#####OnPinchSelect");
        inputModelue.rayTransform = hand.PointerPose;
    }

    /// <summary>
    /// When user cancel pinch, OnUnPinch will be called
    /// </summary>
    /// <param name="layerIndex"></param>
    public void OnUnPinch()
    {
        Debug.Log("#####OnUnPinch");
        inputModelue.rayTransform = null;
    }
}