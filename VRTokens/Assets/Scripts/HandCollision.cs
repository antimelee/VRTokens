using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandCollision : MonoBehaviour
{
    public OVRSkeleton skeleton;
    private bool isAttached = false;

    private OVRBone thumbTip;
    private OVRBone indexTip;
    private GameObject token;
    // Start is called before the first frame update
    void Start()
    {
        thumbTip = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_ThumbTip];
        indexTip = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_IndexTip];   
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 thumpTipPos = skeleton.transform.InverseTransformPoint(thumbTip.Transform.position);
        Vector3 indexTipPos = skeleton.transform.InverseTransformPoint(indexTip.Transform.position);
        if (Vector3.Distance(thumpTipPos, indexTipPos) > 0.06)
        {
            token.transform.parent = null;
            isAttached = false;
        }
    }
    void OnCollisionEnter(Collision collider)
    {
        Debug.Log("#####collision happened");
        if (!isAttached)
        {
            if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) < 0.5f)
            {
                Debug.Log("#####attach tokens to hand");
                token = collider.gameObject;
                token.transform.parent = gameObject.transform;
                isAttached = true;
            }
            //m_renderer.material.color = handIdx == 0 ? m_renderer.material.color = Color.blue : m_renderer.material.color = Color.green;
        }

    }
}
