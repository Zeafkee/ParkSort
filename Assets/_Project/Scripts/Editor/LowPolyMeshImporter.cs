using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace ParkSort.EditorTools
{
    [ScriptedImporter(1, "lpmesh")]
    public sealed class LowPolyMeshImporter : ScriptedImporter
    {
        [Serializable]
        private sealed class Data
        {
            public string name;
            public float[] positions;
            public float[] normals;
            public float[] colors;
            public int[] triangles;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var data = JsonUtility.FromJson<Data>(File.ReadAllText(ctx.assetPath));
            int count = data.positions.Length / 3;

            var positions = new Vector3[count];
            var normals = new Vector3[count];
            var colors = new Color[count];

            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector3(data.positions[i * 3], data.positions[i * 3 + 1], data.positions[i * 3 + 2]);
                normals[i] = new Vector3(data.normals[i * 3], data.normals[i * 3 + 1], data.normals[i * 3 + 2]);

                Color srgb = new Color(data.colors[i * 4], data.colors[i * 4 + 1], data.colors[i * 4 + 2], 1f);
                Color linear = srgb.linear;
                colors[i] = new Color(linear.r, linear.g, linear.b, data.colors[i * 4 + 3]);
            }

            var mesh = new Mesh { name = Path.GetFileNameWithoutExtension(ctx.assetPath) };
            if (count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(data.triangles, 0);
            mesh.RecalculateBounds();

            ctx.AddObjectToAsset("mesh", mesh);
            ctx.SetMainObject(mesh);
        }
    }
}
