using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelMain : MonoBehaviour
{
    public Image image;
    public Button SetImgButton;
    public Text text;
    public Button SetTextButton;
    private int index;

    // Start is called before the first frame update
    void Start()
    {
       image = transform.Find("Image").GetComponent<Image>();
        SetImgButton = transform.Find("SetImgButton").GetComponent<Button>();
        text = transform.Find("Text").GetComponent<Text>();
        SetTextButton = transform.Find("SetTextButton").GetComponent<Button>();
        SetImgButton.onClick.AddListener(OnImgButtonClick);
        SetTextButton.onClick.AddListener(OnTextButtonClick);
    }

    private void OnTextButtonClick()
    {
        index += 20;
        text.text = index.ToString();
    }

    private void OnImgButtonClick()
    {
        if(image.color == Color.green)
        {
            image.color = Color.red;
            return;
        }
        image.color = Color.green;
    }
}
