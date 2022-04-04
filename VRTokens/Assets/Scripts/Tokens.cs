using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;
using Oculus.Interaction.HandPosing;

//[RequireComponent(typeof(Collider))]
public class Tokens : MonoBehaviour
{
    private List<GameObject> tokens = new List<GameObject>();
    private int tokenNum = 1;
    private Vector3 tokenRespawnPos;
    private float tokenWidth = 0.04f;
    private float tokenHeight = 0.02f;
    private Vector3 tableCenter;
    //Set the table size as a unchangeable variable
    private float TABLE_RADIUS = 0.5f;
    //the default rotation of new token
    Quaternion tokenRespawnRot= Quaternion.Euler(0f, 0f, 0f);
    //the dict of each bar. The key is bar's position, value is the list that consist of all token objects
    private Dictionary<GameObject, List<GameObject>> bars = new Dictionary<GameObject, List<GameObject>>();

    //All other tokens are instantiated by the token_prefab
    public GameObject exampleToken;
    public static bool isGrab = false;
    public PhysicMaterial physicalMaterial;
    void Start()
    {
        GameObject table = GameObject.Find("Table");
        tableCenter = table.transform.position;
        Debug.Log("#####table pos: X: " + tableCenter.x + " Z: " + tableCenter.z);
        //The pos of token is at the bottomleft, relative to table.
        tokenRespawnPos.x = tableCenter.x-0.1f;
        tokenRespawnPos.y = tableCenter.y + 0.51f;
        tokenRespawnPos.z = tableCenter.z - 0.45f;
    }

    // Update is called once per frame
    void Update()
    {
        //check for middle finger pinch on the left hand, and make the cube red in this case
        foreach(GameObject token in tokens)
        {
            if(token.GetComponent<OneHandPickTransformer>().isDropped)
            {
                token.GetComponent<OneHandPickTransformer>().isDropped = false;
                Debug.Log("#####token pos: X: " + token.transform.position.x + " Z: " + token.transform.position.z);
                if (IsInsideTable(token.transform.position))
                    MagneticFit(token);
                else
                    DeleteToken(token);
            }
            if (token.GetComponent<OneHandPickTransformer>().isPickedUp)
            {
                token.GetComponent<OneHandPickTransformer>().isPickedUp = false;
                RemoveToken(token);
            }
        }
        
    }

    private void createNewToken()
    {
        GameObject token = Instantiate(exampleToken, tokenRespawnPos, tokenRespawnRot);
        //token.GetComponent<BoxCollider>().material = physicalMaterial;
        token.GetComponent<MeshRenderer>().material.color = PaletteController.newColor;
        tokens.Add(token);
        tokenNum++;

        //Whenever a new token was spawned, it is viewed as a bar.
        List<GameObject> bar = new List<GameObject>();
        bar.Add(token);
        bars.Add(token, bar);
    }

    /*
     * Check if token is still on table.
     */
    private bool IsInsideTable(Vector3 tokenPos)
    {
        if (tokenPos.x < tableCenter.x - TABLE_RADIUS)
            return false;
        else if(tokenPos.x > tableCenter.x + TABLE_RADIUS)
            return false;
        else if (tokenPos.z < tableCenter.z - TABLE_RADIUS)
            return false;
        else if (tokenPos.z > tableCenter.z + TABLE_RADIUS)
            return false;
        return true;
    }
    private void MagneticFit(GameObject token)
    {
        Vector2 tokenXY = new Vector2(token.transform.position.x, token.transform.position.z);
        
        bool isBelongToOneBar = false;
        foreach (var bar in bars)
        {
            GameObject firstTokenofBar = bar.Value[0];
            Vector2 theTokenXY = new Vector2(firstTokenofBar.transform.position.x, firstTokenofBar.transform.position.z);
            // The line below uses a rough detection
            //if (Vector2.Distance(tokenXY, theTokenXY) <= tokenWidth / 2)
            if(RectangleIntersectionDetect(tokenXY, theTokenXY))
            {
                float height = bar.Value.Count * tokenHeight + firstTokenofBar.transform.position.y;
                token.transform.position = new Vector3(theTokenXY.x, height, theTokenXY.y);
                token.transform.rotation = firstTokenofBar.transform.rotation;
                bars[firstTokenofBar].Add(token);
                isBelongToOneBar = true;
                break;
            }
        }
        if (!isBelongToOneBar)
        {
            token.transform.position = new Vector3(token.transform.position.x, tokenRespawnPos.y, token.transform.position.z);
            List<GameObject> bar = new List<GameObject>();
            bar.Add(token);
            bars.Add(token, bar); 
        }
    }

    /*
     * Once a token is picked up, remove the token from token dictionary.
     * The token will be added to dict when it was dropped.
     */
    private void RemoveToken(GameObject token)
    {
        if(bars.ContainsKey(token))
        {
            if(bars[token].Count > 1)
            {
                List<GameObject> newBar = bars[token];
                newBar.Remove(token);
                foreach (var value in newBar)
                { 
                    Vector3 newPos = value.transform.position;
                    newPos.y -= tokenHeight;
                    value.transform.position = newPos;
                }
                bars.Add(newBar[0], newBar);
            }
            bars.Remove(token);
        }
        else
        {
            foreach (var bar in bars)
            {
                if(bar.Value.Contains(token))
                {
                    List<GameObject> chosenBar = bar.Value;
                    bool isHigher = false;
                    foreach(var value in chosenBar)
                    {
                        if(!isHigher)
                            isHigher = value == token;
                        if (isHigher && value!=token)
                        {
                            Vector3 newPos = value.transform.position;
                            newPos.y -= tokenHeight;
                            value.transform.position = newPos;
                        }
                    }
                    bar.Value.Remove(token);
                    break;
                }
            }
        }
    }

    /*
     * Delete the token when a token is dropped outside of the table. Also deleted it from token list.
     */
    private void DeleteToken(GameObject token)
    {
        tokens.Remove(token);
        GameObject.Destroy(token);
    }

        //for test
        public void OnThumbsUp()
    {
        Debug.Log("#####ThumbsUp");
        createNewToken();
    }

    public void OnThumbsDown()
    {
        Debug.Log("#####ThumbsDown");
    }

    /*
     * RectangleIntersectionDetect: Detect if two tokens are intersected
     * Every token is a rectangle.
     * 
     * e.g.:
     *  ############
     *  #          #
     *  #          #
     *  #       ###########
     *  #       #  #      #
     *  ############      #
     *          #         #
     *          #         #
     *          ###########
     * Intersected!
     */
    public bool RectangleIntersectionDetect(Vector2 center1, Vector2 center2)
        {/*
          * BL: Bottom Left
          * TR: Top Right
          */
        Vector2 token1_BL = new Vector2(center1.x - tokenWidth / 2, center1.y - tokenWidth / 2);
        Vector2 token1_TR = new Vector2(center1.x + tokenWidth / 2, center1.y + tokenWidth / 2);
        Vector2 token2_BL = new Vector2(center2.x - tokenWidth / 2, center2.y - tokenWidth / 2);
        Vector2 token2_TR = new Vector2(center2.x + tokenWidth / 2, center2.y + tokenWidth / 2);
        if (token1_TR.x < token2_BL.x || token2_TR.y < token1_BL.y || token2_TR.x < token1_BL.x || token1_TR.y < token2_BL.y)
            return false;
        else
            return true;

    }
    /*    public void OnUnselected()
        {
            Debug.Log("#####No Thumb pose");
        }*/
    //added by Wei
    /*void OnCollisionEnter(Collision collider)
    {
        int handIdx = GetIndexFingerHandId(collider);
        //if there is an associated hand, it means that an index of one of two hands is entering the cube
        //change the color of the cube accordingly (blue for left hand, green for right one)
        if (handIdx != -1 && !isAttached)
        {
            if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) < 0.5f)
            {
                Debug.Log("#####attach tokens to hand");
                transform.parent = collider.gameObject.transform;
                isAttached = true;
                TokenRigidBody.useGravity = false;
                TokenRigidBody.isKinematic = true;
            }
            //m_renderer.material.color = handIdx == 0 ? m_renderer.material.color = Color.blue : m_renderer.material.color = Color.green;
        }
        
    }

    private int GetIndexFingerHandId(Collision collider)
    {
        //Checking Oculus code, it is possible to see that physics capsules gameobjects always end with _CapsuleCollider
        if (collider.gameObject.name.Contains("_CapsuleRigidbody"))
        {
            
            if (collider.transform.IsChildOf(m_hands[0].transform))
                {
                    return 0;
                }
                else if (collider.transform.IsChildOf(m_hands[1].transform))
                {
                    return 1;
                }
        }

        return -1;
    }*/
}