using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuiManager : MonoBehaviour
{
    [SerializeField] private string guiInput = "";

    [SerializeField] private GameObject guiPrefab = null;

    [SerializeField] private GameObject laserPrefab = null;

    [SerializeField] private Transform pointerHandTransform = null;

    [SerializeField] private Camera camera = null;

    private GameObject openGui = null;
    private GameObject guiLaser = null;

    void Update()
    {
        Debug.Log("Input " + Input.GetButton(guiInput));

        if(Input.GetButton(guiInput))
        {
            SpawnGui();
        }
        else
        {
            RemoveGui();
        }
    }

    private void SpawnGui()
    {
        if(openGui == null)
        {
            GameObject gui = Instantiate(guiPrefab);

            VRGuiPointer pointer = gui.GetComponent<VRGuiPointer>();

            pointer.handTransform = pointerHandTransform;
            pointer.camera = camera;
            pointer.guiManager = this;

            gui.transform.SetParent(gameObject.transform, false);
            openGui = gui;

            guiLaser = Instantiate(laserPrefab);
            guiLaser.transform.SetParent(pointerHandTransform, false);
        }
    }

    private void RemoveGui()
    {
        if(openGui != null)
        {
            Destroy(openGui);
            openGui = null;

            Destroy(guiLaser);
            guiLaser = null;
        }
    }

    public void SetOperationMode(OperationType type)
    {
        Debug.Log(type);
    }

    public void SetBrushMode(BrushType type)
    {
        Debug.Log(type);
    }
}
