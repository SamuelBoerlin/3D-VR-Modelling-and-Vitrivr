using ObjLoader.Loader.Loaders;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class QueryResultSpawner : MonoBehaviour, UnityCineastApi.QueryResultCallback
{
    [System.Serializable]
    public struct SpawnSpot
    {
        public Transform transform;
        public Vector3 offset;
    }

    [SerializeField] private GameObject prefab;
    [SerializeField] private float meshSize = 0.5f;
    [SerializeField] private Collider deletionArea;
    [SerializeField] private float deletionAreaRange = 0.1f;
    [SerializeField] private SpawnSpot[] spawnSpots = new SpawnSpot[0];

    public void OnCineastQueryCompleted(List<UnityCineastApi.QueryResult> results)
    {
        if(deletionArea != null)
        {
            //Delete already existing query result objects
            var existing = FindObjectsOfType<QueryResultObject>();
            foreach(QueryResultObject queryResultObject in existing)
            {
                var pos = queryResultObject.gameObject.transform.position;
                if((deletionArea.ClosestPoint(pos) - pos).magnitude < deletionAreaRange)
                {
                    Destroy(queryResultObject.gameObject);
                }
            }
        }

        var factory = new ObjLoaderFactory();

        //Sort by decreasing score
        results.Sort((x, y) => -x.score.CompareTo(y.score));

        for(int j = 0; j < Mathf.Min(results.Count, spawnSpots.Length); j++)
        {
            var result = results[j];

            var loader = factory.Create();

            LoadResult objLoadResult;
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(result.objModel)))
            {
                objLoadResult = loader.Load(stream);
            }

            var queryResultObject = Instantiate(prefab);
            queryResultObject.name = prefab.name + "_" + result.objectDescriptor.ObjectId;

            var queryResultObjectScript = queryResultObject.GetComponent<QueryResultObject>();
            if(queryResultObjectScript == null)
            {
                Debug.LogError("Query result spawner prefab does not have a QueryResultObject component!");
                Destroy(queryResultObject);
                return;
            }

            var meshFilter = queryResultObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.LogError("Query result spawner prefab does not have a MeshFilter component!");
                Destroy(queryResultObject);
                return;
            }

            //Set the query data on the object, e.g. to display the name and score on a canvas
            queryResultObjectScript.SetQueryData(result);

            var spot = spawnSpots[j];
            queryResultObject.transform.position = spot.transform.position + spot.transform.rotation * spot.offset;
            queryResultObject.transform.rotation = spot.transform.rotation;

            Mesh mesh = new Mesh();
            meshFilter.mesh = mesh;

            Vector3 center = Vector3.zero;

            Vector3[] meshVertices = new Vector3[objLoadResult.Vertices.Count];
            for (int i = 0; i < meshVertices.Length; i++)
            {
                var vertex = objLoadResult.Vertices[i];
                center += meshVertices[i] = new Vector3(vertex.X, vertex.Y, vertex.Z);
            }

            center /= meshVertices.Length;

            float maxDistance = 0.0f;

            //Offset so that center is at origin
            for (int i = 0; i < meshVertices.Length; i++)
            {
                var offsetVertex = meshVertices[i] - center;
                maxDistance = Mathf.Max(offsetVertex.magnitude, maxDistance);
                meshVertices[i] = offsetVertex;
            }

            //Scale so that maximum distance from origin == meshSize
            for (int i = 0; i < meshVertices.Length; i++)
            {
                meshVertices[i] = meshVertices[i] / maxDistance * meshSize;
            }

            var meshIndices = new List<int>();
            foreach (var group in objLoadResult.Groups)
            {
                foreach (var face in group.Faces)
                {
                    for (int i = 0; i < face.Count; i++)
                    {
                        meshIndices.Add(face[i].VertexIndex - 1);
                    }
                }
            }

            mesh.vertices = meshVertices;
            mesh.triangles = meshIndices.ToArray();

            mesh.RecalculateNormals();
        }
    }
}
