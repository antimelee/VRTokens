using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TokenControl : MonoBehaviour
{
    //The token 
    [SerializeField]
    private GameObject Token;

    private Color TokenColor;

    private bool IsSelected = false;


    // Start is called before the first frame update
    void Start()
    {
        TokenColor = Token.GetComponent<MeshRenderer>().material.color;
    }

    public void OnRayHover()
    {
        //Debug.Log("#####The OnRayHover was Called");
        Color TransparentColor = TokenColor;
        TransparentColor.a = 0f;
        Token.GetComponent<MeshRenderer>().material.color = TransparentColor;
    }

    /*
     * OnRaySelect monitors the select event triggered by raycaster.
     * For token, it would become transparent when it's selected.
     */
    public void OnRaySelect()
    {
        IsSelected = true;
        //Debug.Log("#####The OnRaySelect was Called");
        Color TransparentColor = TokenColor;
        TransparentColor.a = 0.5f;
        Token.GetComponent<MeshRenderer>().material.color = TransparentColor;
    }

    /*
     * The corresponding function of onRaySelect.
     * The token will become to its origin format (color) when it is unselected.
     */
    public void OnRayUnslect()
    {
        IsSelected = true;
        //Debug.Log("#####The OnRayUnslect was Called");
        Color OriginColor = TokenColor;
        OriginColor.a = 1f;
        Token.GetComponent<MeshRenderer>().material.color = OriginColor;
    }
}
