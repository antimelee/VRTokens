using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

/*
 * The length of the token is same to the width, while differen with height
 */
public class Tokens : MonoBehaviour
{
    #region
    private List<GameObject> tokens = new List<GameObject>();
    private int tokenNum = 1;
    private Vector3 tokenRespawnPos;
    //The width of a single token, which is 2.5cm
    private float tokenWidth;
    //The height of a single token, which is 0.3mm
    private float tokenHeight;
    private Vector3 tableCenter;
    //Set the table size, the table is a rectangle, the radius if the half length of on edge, which is 0.5m
    private float TABLE_RADIUS = 0.5f;
    //the default rotation of new token
    Quaternion tokenRespawnRot= Quaternion.Euler(0f, 0f, 0f);
    //the dict of each bar. The key is bar's container, value is the list that consist of all token objects
    private Dictionary<GameObject, List<GameObject>> bars = new Dictionary<GameObject, List<GameObject>>();
    private float tableLevel = 0.5f;
    #endregion

    #region
    //All other tokens are instantiated by the token_prefab
    public GameObject exampleToken;
    public GameObject barContainerPref;
    public static bool isGrab = false;
    #endregion
    void Start()
    {
        GameObject table = GameObject.Find("Table");
        Vector3 tokenSpawnPos = GameObject.Find("SpawnPlace3D").transform.position;
        tableCenter = table.transform.position;
        //The pos of token is at the bottomleft, relative to table.
        tokenRespawnPos.x = tokenSpawnPos.x;
        tokenRespawnPos.y = tokenSpawnPos.y + 0.01f+tokenHeight/2;
        tokenRespawnPos.z = tokenSpawnPos.z;
        tokenWidth = exampleToken.transform.localScale.x;
        tokenHeight = exampleToken.transform.localScale.y;
        DefaultTokenPool();
    }

    // Update is called once per frame
    void Update()
    {
        foreach(GameObject bar in bars.Keys)
        {
            if(bar.GetComponent<Grabbable>().IsReleased)
            {
                bar.GetComponent<Grabbable>().IsReleased = false;
                if (IsInsideTable(bar.transform.position))
                    MagneticFit(bar, false);
                else
                    DeleteBar(bar);
            }
            if (bar.GetComponent<Grabbable>().IsGrabbed)
            {
                bar.GetComponent<Grabbable>().IsGrabbed = false;
            }
        }
        /*
         Possible new strategy: create a new data format for token that's not part of a bar. 
         And only iterate these tokens
         */
        foreach(GameObject token in tokens)
        {
            if(token.GetComponent<Grabbable>().IsReleased)
            {
                token.GetComponent<Grabbable>().IsReleased = false;
                if (IsInsideTable(token.transform.position))
                    MagneticFit(token, true);
                else
                    DeleteToken(token);
            }
/*            if (token.GetComponent<Grabbable>().IsGrabbed)
            {
                token.GetComponent<Grabbable>().IsGrabbed = false;
                //RemoveToken(token);
            }*/
        }
        
    }


    /*
     * Generate the default 8 piles of token, each pile has 10 tokens
     */
    private void DefaultTokenPool()
    {
        //color code of 8 main colors: Red, Orange, Yellow, Green, Blue, Purple, Brown, Black
        List<Vector3> ColorCodes = new List<Vector3> { new Vector3(1f, 0f, 0f), new Vector3(1f, 165/255f, 0f), new Vector3(1f, 1f, 0f), new Vector3(0f, 1f, 0f),
            new Vector3(0f, 0f, 1f), new Vector3(128/255f, 0f, 128/255f), new Vector3(165/255f, 42/255f, 42/255f),new Vector3(0f, 0f, 0f)};
        int DefaultNum = 10;
        Vector3 RightHangPos = GameObject.Find("Table/Right").transform.position;
        Vector3 StartPos = new Vector3(RightHangPos.x - 0.1f, tableLevel + tokenHeight / 2, RightHangPos.z + 0.12f);
        for (int i = 0; i < 8; i++)
        {
            StartPos.y = tableLevel + tokenHeight / 2;
            Color tokenColor = new Color(ColorCodes[i].x, ColorCodes[i].y, ColorCodes[i].z);
            for (int j = 0; j < DefaultNum; j++)
            {
                GameObject token = Instantiate(exampleToken, StartPos, tokenRespawnRot);
                token.GetComponent<MeshRenderer>().material.color = tokenColor;
                tokens.Add(token);
                tokenNum++;
                StartPos.y += tokenHeight;
            }
            StartPos.z -= tokenWidth + 0.005f;
        }
    }
    private void CreateNewToken()
    {
        GameObject token = Instantiate(exampleToken, tokenRespawnPos, tokenRespawnRot);
        token.GetComponent<MeshRenderer>().material.color = PaletteController.newColor;
        tokens.Add(token);
        tokenNum++;
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

    /// <summary>
    /// Simulate the free fall but make sure the object is always connected to the nearest one perfectly.
    /// currently, only work for 3D barchart.
    /// </summary>
    /// <param name="token"></param> <param name="grabbedObject"></param>
    private void MagneticFit(GameObject grabbedObject, bool isToken)
    {
        Vector2 objectXY = new Vector2(grabbedObject.transform.position.x, grabbedObject.transform.position.z);
        //if the grabbaedObject is a bar, use tentativeBarTokens to store the tokens of that bar
        List<GameObject> tentativeBarTokens = new List<GameObject>();
        if (!isToken)
        {
            tentativeBarTokens = bars[grabbedObject];
            RemoveBar(grabbedObject);
        }
        bool isBelongToOneBar = false;
        foreach (var bar in bars)
        {
            GameObject firstTokenofBar = bar.Value[0];
            Vector2 theTokenXY = new Vector2(firstTokenofBar.transform.position.x, firstTokenofBar.transform.position.z);
            // The line below uses a rough detection
            if (RectangleIntersectionDetect(objectXY, theTokenXY))
            {
                isBelongToOneBar = true;
                if (!isToken)
                { 
                    MergeTwoBars(bar.Key, grabbedObject, tentativeBarTokens);
                    GameObject.Destroy(grabbedObject);
                }
                else
                {
                    float height = bar.Value.Count * tokenHeight + firstTokenofBar.transform.position.y;
                    grabbedObject.transform.position = new Vector3(theTokenXY.x, height, theTokenXY.y);
                    grabbedObject.transform.rotation = firstTokenofBar.transform.rotation;
                    //Makes token uninteractable.
                    grabbedObject.GetComponentInChildren<HandGrabInteractable>().InjectSupportedGrabTypes(Oculus.Interaction.Grab.GrabTypeFlags.None);
                    bars[bar.Key].Add(grabbedObject);
                    AddTokenToContainer(bar.Key);
                }
                break;
            }
        }
        if (!isBelongToOneBar)
        {
            if (isToken)
            {
                grabbedObject.transform.position = new Vector3(grabbedObject.transform.position.x, 0.5f + tokenHeight / 2, grabbedObject.transform.position.z);
                grabbedObject.transform.rotation = exampleToken.transform.rotation;
                grabbedObject.GetComponentInChildren<HandGrabInteractable>().InjectSupportedGrabTypes(Oculus.Interaction.Grab.GrabTypeFlags.None);
                List<GameObject> newbar = new List<GameObject>();
                newbar.Add(grabbedObject);
                bars.Add(CreateBarContainer(newbar), newbar);
            }
            else
            {
                grabbedObject.transform.position = new Vector3(grabbedObject.transform.position.x, 0.5f + tentativeBarTokens.Count * tokenHeight / 2, grabbedObject.transform.position.z);
                grabbedObject.transform.rotation = exampleToken.transform.rotation;
                bars.Add(grabbedObject, tentativeBarTokens);
            }
        }
    }

    /*
     * The function is not called, not sure if we want allocate how much freedom to a single token.
     * Once a token is picked up, remove the token from token dictionary.
     * The token will be added to dict when it was dropped.
     */
    private void RemoveToken(GameObject token)
    {
        if (bars.ContainsKey(token))
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

    private void RemoveBar(GameObject bar)
    {
        if (bars.ContainsKey(bar))
            bars.Remove(bar);
        else
            Debug.Log("##### ErrorWEI: The bar cannot be found in bars");
    }

    /*
     * Delete the token when a token is dropped outside of the table. Also deleted it from token list.
     */
    private void DeleteToken(GameObject token)
    {
        tokens.Remove(token);
        GameObject.Destroy(token);
    }

    private void DeleteBar(GameObject bar)
    {
        foreach (var token in bars[bar])
            DeleteToken(token);
        bars.Remove(bar);
        GameObject.Destroy(bar);
    }

    //for test
    public void OnThumbsUp()
    {
        Debug.Log("#####ThumbsUp");
        CreateNewToken();
    }

    public void OnThumbsDown()
    {
        Debug.Log("#####ThumbsDown");
    }

    public void OnRayHover(GameObject token)
    {
        token.GetComponent<MeshRenderer>().material.color = PaletteController.newColor;
        Color tokenColor = token.GetComponent<MeshRenderer>().material.color;
        tokenColor.a = 0.5f;
        token.GetComponent<MeshRenderer>().material.color = tokenColor;
        Debug.Log("#####OnRayHover");
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
     * When return value is -1, the new token won't be attached to a exsiting bar
     * When return value is less than 0.75tokenwidth, the new token will be attached to the top of a exsiting bar, form a 3D bar chart
     * When return value between 0.75tokenwidth and 1.25tokenwidth, the new token will be attached to the edge of a exsiting bar, form a 2D bar chart
     * When return value is larger than 1.25tokenwidth, the new token won't be attached to a exsiting bar
     */
    public bool RectangleIntersectionDetect(Vector2 center1, Vector2 center2)
   {
        /*
          * BL: Bottom Left
          * TR: Top Right
          */
        Vector2 token1_BL = new Vector2(center1.x - tokenWidth / 2, center1.y - tokenWidth / 2);
        Vector2 token1_TR = new Vector2(center1.x + tokenWidth / 2, center1.y + tokenWidth / 2);
        Vector2 token2_BL = new Vector2(center2.x - tokenWidth / 2, center2.y - tokenWidth / 2);
        Vector2 token2_TR = new Vector2(center2.x + tokenWidth / 2, center2.y + tokenWidth / 2);
        //float yDistance = Math.Abs(center1.y - center2.y);
        if (token1_TR.x < token2_BL.x || token2_TR.y < token1_BL.y || token2_TR.x < token1_BL.x || token1_TR.y < token2_BL.y)
        {
            return false;
        }
        else
            return true;

    }

    /*
     * When a new bar was created, its container should also be generated.
     * The container is used for scablable interaction. It will have box collider, rigidbody, grabbable, and handgrabinteractable
     */
    private GameObject CreateBarContainer(List<GameObject> bar)
    {
        GameObject firstToken = bar[0];
        //generate the container from prefab, which is a cube gameobject.

        GameObject container = Instantiate(barContainerPref, tokenRespawnPos, tokenRespawnRot);
        Vector3 exampleScale = exampleToken.transform.localScale;
        container.transform.localScale = exampleScale;
        container.transform.rotation = firstToken.transform.rotation;
        container.transform.position = firstToken.transform.position;
        foreach (GameObject token in bar)
            token.transform.parent = container.transform;
        return container;
    }

    /*
     * After the new token is attached to the bar
     * Add it to bar container
     * This step doesn't involve any data manipulation. It only changes the size of barcontainer and repositiones it.
     */
    private void AddTokenToContainer(GameObject barContainer)
    {
        //GameObject firstToken = bar[0];
        //generate the container, which is a cube gameobject.
        //float posX, posY, posZ;
        //if it's a 2D bar chart
/*        if (firstToken.transform.position.y == newToken.transform.position.y)
        {
            posX = firstToken.transform.position.x + newToken.transform.position.x;
            posZ = firstToken.transform.position.z + newToken.transform.position.z;
            posX /= 2;
            posZ /= 2;
            posY = firstToken.transform.position.y;
            barContainer.transform.position = new Vector3(posX, posY, posZ);
            if (firstToken.transform.localPosition.x == newToken.transform.localPosition.x)
            {
                float newLocalScaleZ = barContainer.transform.localScale.z + exampleToken.transform.localScale.z;
                Vector3 oldScale = barContainer.transform.localScale;
                barContainer.transform.localScale = new Vector3(oldScale.x, oldScale.y, newLocalScaleZ);
                barContainer.GetComponent<BoxCollider>().size = new Vector3(oldScale.x, oldScale.y, newLocalScaleZ);
            }
            else
            {
                float newLocalScaleX = barContainer.transform.localScale.x + exampleToken.transform.localScale.x;
                Vector3 oldScale = barContainer.transform.localScale;
                barContainer.transform.localScale = new Vector3(newLocalScaleX, oldScale.y, oldScale.z);
                barContainer.GetComponent<BoxCollider>().size = new Vector3(newLocalScaleX, oldScale.y, oldScale.z);
            }
        }
        else*/
        //{
        Vector3 oldScale = barContainer.transform.localScale;
        Vector3 oldPos = barContainer.transform.position;
        float newLocalScaleY = oldScale.y + exampleToken.transform.localScale.y;
        float newLocalPosY = oldPos.y + exampleToken.transform.localScale.y/2;
        //unparenting tokens
        foreach (GameObject token in bars[barContainer])
            token.transform.parent = null;
        barContainer.transform.localScale = new Vector3(oldScale.x, newLocalScaleY, oldScale.z);
        barContainer.transform.position = new Vector3(oldPos.x, newLocalPosY, oldPos.z);
        barContainer.GetComponent<BoxCollider>().size = new Vector3(oldScale.x, newLocalScaleY, oldScale.z);
        //}
        //reparenting all tokens
        foreach (GameObject token in bars[barContainer])
            token.transform.parent = barContainer.transform;
    }
    /*
     * The bar1 is the bar that's in bars, the bar2 is the bar that is grabbed and deleted from bars.
     */
    private void MergeTwoBars(GameObject bar1, GameObject bar2, List<GameObject> tokenOfBar2)
    {
        Vector3 newScale = bar1.transform.localScale;
        newScale.y += bar2.transform.localScale.y;
        Vector3 newPos = bar1.transform.position;
        bar2.transform.position = new Vector3(newPos.x, tableLevel + (2*bars[bar1].Count + tokenOfBar2.Count)*tokenHeight/2, newPos.z);
        bar2.transform.rotation = exampleToken.transform.rotation;
        if (bar1.transform.position.y == bar2.transform.position.y)
        {
            //To be added
        }
        else
            newPos.y += bar2.transform.localScale.y / 2;
        
        foreach(var token in tokenOfBar2)
            bars[bar1].Add(token);
        
        //unparenting tokens
        foreach (GameObject token in bars[bar1])
            token.transform.parent = null;
        bar1.transform.position = newPos;
        bar1.transform.localScale = newScale;
        bar1.GetComponent<BoxCollider>().size = newScale;
        //reparenting all tokens
        foreach (GameObject token in bars[bar1])
            token.transform.parent = bar1.transform;

        Debug.Log("##### Bars were merged");
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