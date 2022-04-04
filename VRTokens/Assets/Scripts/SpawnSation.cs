using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSation : MonoBehaviour
{
    //Only used to check if collision happens
    public static bool isTouched = false;

    private OVRHand[] m_hands;
    private void Awake()
    {
        m_hands = new OVRHand[]
        {
            GameObject.Find("OVRCameraRig/TrackingSpace/LeftHandAnchor/OVRHandPrefab").GetComponent<OVRHand>(),
            GameObject.Find("OVRCameraRig/TrackingSpace/RightHandAnchor/OVRHandPrefab").GetComponent<OVRHand>()
        };
    }
    private void OnTriggerEnter(Collider other)
    {
        
        if (other.transform.IsChildOf(m_hands[1].transform))
        {
            isTouched = true;
        }
    }
    public void OnThumbsUp()
    {
        Debug.Log("#####ThumbsUp");
    }

    public void OnThumbsDown()
    {
        Debug.Log("#####ThumbsDown");
    }

    public void OnUnselected()
    {
        Debug.Log("#####No Thumb pose");
    }
}
