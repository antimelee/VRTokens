using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PaletteController : MonoBehaviour
{
    // Start is called before the first frame update

    static public Color newColor;
    public TextMeshPro Rtext;
    public TextMeshPro Gtext;
    public TextMeshPro Btext;

    private GameObject RSliderPoint;
    private GameObject GSliderPoint;
    private GameObject BSliderPoint;
    private GameObject ColorCube;

    public float SliderRange = 0.2f;
    public float SliderStartPos = -0.1f;

    private int COLOR_RANGE=255;
    void Start()
    {

        RSliderPoint = GameObject.Find("Palette/RSlider/Sphere");
        GSliderPoint = GameObject.Find("Palette/GSlider/Sphere");
        BSliderPoint = GameObject.Find("Palette/BSlider/Sphere");
        ColorCube = GameObject.Find("Palette/ColorCube");
    }

    // Update is called once per frame
    void Update()
    {
        int RValue = Mathf.RoundToInt(COLOR_RANGE*(RSliderPoint.transform.localPosition.x- SliderStartPos) / SliderRange);
        Rtext.text = RValue.ToString();
        int GValue = Mathf.RoundToInt(COLOR_RANGE * (GSliderPoint.transform.localPosition.x - SliderStartPos) / SliderRange);
        Gtext.text = GValue.ToString();
        int BValue = Mathf.RoundToInt(COLOR_RANGE * (BSliderPoint.transform.localPosition.x - SliderStartPos) / SliderRange);
        Btext.text = BValue.ToString();
        newColor = new Color(RValue / 255f, GValue / 255f, BValue / 255f);
        ColorCube.GetComponent<MeshRenderer>().material.color = newColor;
        //ExampleToken.GetComponent<MeshRenderer>().material.color = newColor;
    }
}
