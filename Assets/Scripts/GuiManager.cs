using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuiManager : MonoBehaviour
{
    [SerializeField] private string guiInput = "";

    [SerializeField] private string triggerInput = "";

    [SerializeField] private GameObject guiPrefab = null;

    [SerializeField] private GameObject laserPrefab = null;

    [SerializeField] private Transform pointerHandTransform = null;

    [SerializeField] private Camera camera = null;

    private GameObject openGui = null;
    private GameObject guiLaser = null;

    private bool wasTriggerDown = false;

    private OperationType opType = OperationType.Union;
    private BrushType brushType = BrushType.Cube;

    private float startScale;

    private void Start()
    {
        startScale = GameObject.FindGameObjectWithTag("Sculpture").transform.localScale.x;
    }

    void Update()
    {
        if(Input.GetButton(guiInput))
        {
            SpawnGui();
        }
        else
        {
            RemoveGui();
        }

        bool isTriggerDown = Input.GetAxis(triggerInput) > 0.5f;
        if(wasTriggerDown != isTriggerDown)
        {
            if(openGui == null && isTriggerDown)
            {
                GameObject sculpture = GameObject.FindGameObjectWithTag("Sculpture");
                if (sculpture != null)
                {
                    Sculpture script = sculpture.GetComponent<Sculpture>();

                    if(script != null)
                    {
                        int material;
                        if(opType == OperationType.Union)
                        {
                            material = 1;
                        }
                        else
                        {
                            material = 0;
                        }

                        ISdf shape;
                        if(brushType == BrushType.Cube)
                        {
                            shape = new BoxSDF(8.0f);
                        }
                        else
                        {
                            shape = new SphereSDF(8.0f);
                        }

                        shape = new ScaleSDF(1.0f / (sculpture.transform.localScale.x / startScale), shape);

                        script.ApplySdf(pointerHandTransform.position + pointerHandTransform.rotation * new Vector3(0, 0, 0.3f), pointerHandTransform.rotation, shape, material, false);
                    }
                }
            }

            wasTriggerDown = isTriggerDown;
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
        opType = type;
    }

    public void SetBrushMode(BrushType type)
    {
        Debug.Log(type);
        brushType = type;
    }
}
