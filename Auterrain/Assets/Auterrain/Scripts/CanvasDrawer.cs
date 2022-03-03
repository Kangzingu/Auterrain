using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
public class CanvasDrawer : MonoBehaviour
{
    // Start is called before the first frame update
    public RawImage image;
    public Dropdown penThicknessDropDown;
    Texture2D texture;
    Vector2 exMousePos;

    int penThickness = 1;

    float m;
    float b;
    float smallerVal;
    float biggerVal;
    void Start()
    {
        texture = new Texture2D(256, 256);
        for (int i = 0; i < 256; i++)
        {
            for (int j = 0; j < 256; j++)
            {
                texture.SetPixel(i, j, new Color(0, 0, 0));
            }
        }
        texture.Apply();
        image.texture = texture;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            exMousePos = Input.mousePosition;
        }
       
        
        if (Input.GetMouseButton(0))
        {
            if ((0 <= Input.mousePosition.x) && (Input.mousePosition.x <= 256) &&
                (0 <= Input.mousePosition.y) && (Input.mousePosition.y <= 256))
            {
                m = (Input.mousePosition.y - exMousePos.y) / (Input.mousePosition.x - exMousePos.x);
                if (Input.mousePosition.x - exMousePos.x ==0)
                {
                    smallerVal = Input.mousePosition.y > exMousePos.y ? exMousePos.y : Input.mousePosition.y;
                    biggerVal = Input.mousePosition.y > exMousePos.y ? Input.mousePosition.y : exMousePos.y;
                    for (int i = (int)smallerVal; i <= (int)biggerVal; i++)
                    {
                        for (int j = 0; j < penThickness; j++)
                        {
                            for (int k = 0; k < penThickness; k++)
                            {
                                texture.SetPixel((int)exMousePos.x+j, i+k, new Color(255, 255, 255));
                            }
                        }
                    }

                }
                else
                {
                    b = Input.mousePosition.y - m * Input.mousePosition.x;
                    if (Mathf.Abs(m) > 1)
                    {
                        //y가 유리함
                        smallerVal = Input.mousePosition.y > exMousePos.y ? exMousePos.y : Input.mousePosition.y;
                        biggerVal = Input.mousePosition.y > exMousePos.y ? Input.mousePosition.y : exMousePos.y;
                        for (int i = (int)smallerVal; i <= (int)biggerVal; i++)
                        {
                            for(int j = 0; j < penThickness; j++)
                            {
                                for(int k = 0; k < penThickness; k++)
                                {
                                    texture.SetPixel((int)((i - b) / m)+j, i+k, new Color(255, 255, 255));
                                }
                            }
                        }
                    }
                    else
                    {
                        smallerVal = Input.mousePosition.x > exMousePos.x ? exMousePos.x : Input.mousePosition.x;
                        biggerVal = Input.mousePosition.x > exMousePos.x ? Input.mousePosition.x : exMousePos.x;
                        for (int i = (int)smallerVal; i <= (int)biggerVal; i++)
                        {
                            for (int j = 0; j < penThickness; j++)
                            {
                                for (int k = 0; k < penThickness; k++)
                                {
                                    texture.SetPixel(i+j, (int)(m * i + b)+k, new Color(255, 255, 255));
                                }
                            }
                        }
                    }
                }
                texture.Apply();
                image.texture = texture;
            }
            exMousePos = Input.mousePosition;
        }
    }
    public void DoChangeThickness()
    {
        penThickness = penThicknessDropDown.value + 1;
    }
    public void DoSaveJPG()
    {
        StartCoroutine(SaveAsJPG());
    }
    IEnumerator SaveAsJPG()
    {
        yield return new WaitForSeconds(0);
        var imageData = texture.EncodeToJPG();
        File.WriteAllBytes(Application.dataPath + "/Auterrain/InputSketchImage/SavedImage.jpg", imageData);
        UnityEngine.Debug.Log("[Notice] Image Saved - " + Application.dataPath + "/Auterrain/InputSketchImage/SavedImage.jpg");
    }
    public void DoEraseAll()
    {
        for (int i = 0; i < 256; i++)
        {
            for (int j = 0; j < 256; j++)
            {
                texture.SetPixel(i, j, new Color(0, 0, 0));
            }
        }
        texture.Apply();
        image.texture = texture;
    }
}