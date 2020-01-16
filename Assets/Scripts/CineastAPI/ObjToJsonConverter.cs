using Newtonsoft.Json.Linq;
using ObjLoader.Loader.Loaders;
using System.IO;

namespace Cineast_OpenAPI_Implementation
{
    public class ObjToJsonConverter
    {
        public static string Convert(Stream stream)
        {
            var factory = new ObjLoaderFactory();
            var loader = factory.Create();

            var result = loader.Load(stream);

            var jsonVertices = new JArray();
            var json = new JObject(new JProperty("vertices", jsonVertices));

            foreach (var group in result.Groups)
            {
                foreach (var face in group.Faces)
                {
                    for (int i = 0; i < face.Count; i++)
                    {
                        var vert = result.Vertices[face[i].VertexIndex - 1];
                        jsonVertices.Add(vert.X);
                        jsonVertices.Add(vert.Y);
                        jsonVertices.Add(vert.Z);
                    }
                }
            }

            return json.ToString();
        }
    }
}