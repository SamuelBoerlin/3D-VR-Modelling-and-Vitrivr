using Newtonsoft.Json.Linq;
using UnityEngine;

public class SculptureToJsonConverter
{
    public static string Convert(Sculpture sculpture)
    {
        var jsonVertices = new JArray();
        var json = new JObject(new JProperty("vertices", jsonVertices));

        foreach (var chunk in sculpture.GetChunks())
        {
            var mesh = chunk.mesh;
            if (mesh != null)
            {
                for(int i = 0; i < mesh.triangles.Length - 3; i++)
                {
                    var index = mesh.triangles[i];
                    var pos = mesh.vertices[index];
                    jsonVertices.Add(pos.x + chunk.Pos.x * chunk.ChunkSize);
                    jsonVertices.Add(pos.y + chunk.Pos.y * chunk.ChunkSize);
                    jsonVertices.Add(pos.z + chunk.Pos.z * chunk.ChunkSize);
                }
            }
        }

        return json.ToString();
    }
}