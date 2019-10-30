using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabberLogic : MonoBehaviour
{
    [SerializeField] private string triggerInput;

    private Vector3 lastGrabberPos;

    private void OnCollisionStay(Collision collision)
    {
        if (Input.GetAxis(triggerInput) > 0.5)
        {
            var otherObject = collision.gameObject;
            otherObject.GetComponent<Transform>().SetParent(this.gameObject.GetComponent<Transform>());
            Rigidbody rb = otherObject.GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = true;
        }
    }

    private void FixedUpdate()
    {
        var transform = this.gameObject.GetComponent<Transform>();

        Vector3 velocity = (transform.position - lastGrabberPos) / Time.fixedDeltaTime;

        Debug.Log("Input: " + Input.GetAxis(triggerInput));

        if (Input.GetAxis(triggerInput) < 0.5)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                child.SetParent(null, true);
                Rigidbody rb = child.gameObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.velocity = velocity;
                }
            }
        }

        this.lastGrabberPos = transform.position;
    }
}
