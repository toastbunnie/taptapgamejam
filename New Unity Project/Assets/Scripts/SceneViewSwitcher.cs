using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneViewSwitcher : MonoBehaviour
{
    public GameObject view0;
    public GameObject view1;

    public GameObject leftButton;
    public GameObject rightButton;

    private void Start()
    {
        ShowView0();
    }

    public void ShowView0()
    {
        view0.SetActive(true);
        view1.SetActive(false);

        leftButton.SetActive(false);
        rightButton.SetActive(true);
    }

    public void ShowView1()
    {
        view0.SetActive(false);
        view1.SetActive(true);

        leftButton.SetActive(true);
        rightButton.SetActive(false);
    }
}