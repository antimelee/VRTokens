using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TokenCollisionDetector : MonoBehaviour
{
    /// <summary>
    /// Reference to the managers of the hands.
    /// First item is left hand, second item is right hand
    /// </summary>
    void OnCollisionEnter(Collision collider)
    {
        Debug.Log("###### TokenCollisionDetector: collision happened: " + collider.gameObject.name);
        
        //following if statement is a bad check, however it's the only common part of all the capsule colliders of OVRHand
        if (collider.gameObject.name.Contains("b_r_index3_CapsulePhysics"))
        {
            //this.gameObject.tag = "Grabbed";
            if (Tokens.isGrab)
            { 
                this.gameObject.transform.parent = collider.gameObject.transform;
                this.gameObject.GetComponent<Rigidbody>().useGravity = false;
                this.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            }
        }
        //m_renderer.material.color = handIdx == 0 ? m_renderer.material.color = Color.blue : m_renderer.material.color = Color.green;
    }

}
