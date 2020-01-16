using Cineast_OpenAPI_Implementation;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class QueryPlate : MonoBehaviour
{
    [SerializeField] private UnityCineastApi cineastApi;

    [SerializeField] private QueryResultSpawner.SpawnSpot pedestalSpot;

    private GameObject pedestalObject;

    private void OnCollisionEnter(Collision collision)
    {
        var queryResultObject = collision.gameObject.GetComponent<QueryResultObject>();
        if (queryResultObject != null)
        {
            Debug.Log("Starting new query!");

            using (Stream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(queryResultObject.QueryResult.objModel)))
            {
                cineastApi.StartQuery(ObjToJsonConverter.Convert(stream));
            }

            if(pedestalObject != null)
            {
                Destroy(pedestalObject);
            }
            pedestalObject = collision.gameObject;
            pedestalObject.transform.position = pedestalSpot.transform.position + pedestalSpot.transform.rotation * pedestalSpot.offset;
            pedestalObject.transform.rotation = pedestalSpot.transform.rotation;
        }
    }
}
