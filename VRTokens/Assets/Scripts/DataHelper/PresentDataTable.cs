using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using System;
using Microsoft.VisualBasic.FileIO;

public class PresentDataTable : MonoBehaviour
{

    public GameObject Row_Prefab;
    public GameObject Cell_Prefab;
    public string Csv_Filename;

    private int RowNumber;
    private int ColNumber;
    private List<List<string>> Data = new List<List<string>>();
    // Start is called before the first frame update
    void Start()
    {
        ReadCSV(Csv_Filename);
        ShowData();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void ShowData()
    {
        for(int i = 0; i < RowNumber; i++)
        {
            Debug.Log("Data " + Data[i][0]);
            GameObject contentTable = GameObject.Find("Scroll View/Viewport/Content");
            Vector3 originPos = contentTable.transform.position;
            GameObject row = GameObject.Instantiate(Row_Prefab, originPos, contentTable.transform.rotation);
            row.name = "row" + i;
            row.transform.SetParent(contentTable.transform);
            for (int j = 0; j < ColNumber; j++)
            { 
                if (Data[i][j] != null)
                {
                    originPos.x += j * 120;
                    GameObject cell = GameObject.Instantiate(Cell_Prefab, originPos, contentTable.transform.rotation);
                    cell.GetComponent<TMP_InputField>().text = Data[i][j].ToString();
                    cell.name = "cell" + j;
                    cell.transform.SetParent(row.transform);
                }
            }
        }
    }

    private void ReadCSV(string Csv_Filename)
    {
        /*The following code only works for local file*/
        string path = "Assets/CSV/" + Csv_Filename;
        using (TextFieldParser parser = new TextFieldParser(path))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.HasFieldsEnclosedInQuotes = true;
            parser.SetDelimiters(",");

            while (!parser.EndOfData)
            {
                List<string> list = new List<string>();
                string[] row = parser.ReadFields();
                foreach (string field in row)
                {
                    list.Add(field);
                }
                Data.Add(list);
            }
        }
        /*TextAsset f = (TextAsset)Resources.Load("Csv/" + Csv_Filename);
        String fileText = System.Text.Encoding.UTF8.GetString(f.bytes);
        Debug.Log("Row: " + fileText);
        char[] separator = {'\n' };
        string[] rows = fileText.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        foreach (string row in rows)
        {
            Debug.Log("Row: " + row);
            List<string> oneRow = new List<string>(row.Split(','));
            Data.Add(oneRow);
        }*/
        RowNumber = Data.Count;
        ColNumber = Data[0].Count;
    }
}
