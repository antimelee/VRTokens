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
public class Tokens2D : MonoBehaviour
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
        Vector3 tokenSpawnPos = GameObject.Find("SpawnPlace2D").transform.position;
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
        Vector3 StartPos = new Vector3(RightHangPos.x + 0.1f, tableLevel + tokenHeight / 2, RightHangPos.z + 0.12f);
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
            GameObject lastTokenofBar = bar.Value[bar.Value.Count-1];
            Vector2 theTokenXY = new Vector2(lastTokenofBar.transform.position.x, lastTokenofBar.transform.position.z);
            // The line below uses a rough detection
            if (TokenCloseDetect(objectXY, theTokenXY))
            {
                isBelongToOneBar = true;
                if (!isToken)
                { 
                    MergeTwoBars(bar.Key, grabbedObject, tentativeBarTokens);
                    GameObject.Destroy(grabbedObject);
                }
                else
                {
                    float length = bar.Value.Count * tokenWidth + lastTokenofBar.transform.position.z;
                    grabbedObject.transform.position = new Vector3(theTokenXY.x, tokenHeight/2+tableLevel, length);
                    grabbedObject.transform.rotation = lastTokenofBar.transform.rotation;
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

    private bool TokenCloseDetect(Vector2 pos1, Vector2 pos2)
    {
        return (pos1.x >= pos2.x - tokenWidth / 2 && pos1.x <= pos2.x + tokenWidth / 2);
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
        Vector3 oldScale = barContainer.transform.localScale;
        Vector3 oldPos = barContainer.transform.position;
        float newLocalScaleZ = oldScale.z + exampleToken.transform.localScale.z;
        float newLocalPosZ = oldPos.z + exampleToken.transform.localScale.z/2;
        //unparenting tokens
        foreach (GameObject token in bars[barContainer])
            token.transform.parent = null;
        barContainer.transform.localScale = new Vector3(oldScale.x, oldScale.y, newLocalScaleZ);
        barContainer.transform.position = new Vector3(oldPos.x, oldPos.y, newLocalPosZ);
        barContainer.GetComponent<BoxCollider>().size = new Vector3(oldScale.x, oldScale.y, newLocalScaleZ);
        //}
        //reparenting all tokens
        foreach (GameObject token in bars[barContainer])
            token.transform.parent = barContainer.transform;
    }
    /*
     * The bar1 is the barcontainer that's in bars, the bar2 is the barcontainer that is grabbed and deleted from bars.
     */
    private void MergeTwoBars(GameObject bar1, GameObject bar2, List<GameObject> tokenOfBar2)
    {
        Vector3 newScale = bar1.transform.localScale;
        newScale.z += bar2.transform.localScale.z;
        Vector3 newPos = bar1.transform.position;
        bar2.transform.position = new Vector3(newPos.x, newPos.y, newPos.z + (2 * bars[bar1].Count -1 + tokenOfBar2.Count) * tokenHeight / 2);
        bar2.transform.rotation = exampleToken.transform.rotation;
        newPos.z += bar2.transform.localScale.z / 2;
        
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
    }