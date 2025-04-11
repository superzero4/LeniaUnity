using System.Collections.Generic;
using System.IO;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace.Tools
{
    public class VoxelToTex : MonoBehaviour
    {
        [SerializeField] private Texture3DSO _texture;
        [SerializeField] private string _path;
        [SerializeField] private string _name;
        [SerializeField, Range(0, 1f)] private float _yOffset = 0f;
        [SerializeField] private bool _ignoreSourceColor = false;

        private struct Voxel
        {
            public Vector3Int pos;
            public Color color;
        }

        [Button]
        private void Process()
        {
            List<Voxel> voxels = new List<Voxel>();
            Vector3Int max = new Vector3Int(0, 0, 0);
            using (var stream = new StreamReader(File.OpenRead(_path + _name)))
            {
                while (!stream.EndOfStream)
                {
                    var split = stream.ReadLine().Split(',').Select(s => int.Parse(s)).ToArray();
                    Voxel v = new Voxel()
                    {
                        pos = new Vector3Int(split[0], split[1], split[2]),
                        color = _ignoreSourceColor
                            ? new Color(1f, 1f, 1f, 1f)
                            : new Color(split[3] / 255f, split[4] / 255f, split[5] / 255f)
                    };
                    max.x = Mathf.Max(max.x, v.pos.x);
                    max.y = Mathf.Max(max.y, v.pos.y);
                    max.z = Mathf.Max(max.z, v.pos.z);
                    voxels.Add(v);
                    //if(UnityEngine.Random.Range(0f, 1f)<.05f)
                    //    Debug.Log("Info : " + v.pos.x + "," + v.pos.y + "," + v.pos.z + " : " + v.color);
                }
            }

            int globalMax = Mathf.Max(max.x, max.y, max.z);
            int padding = Mathf.CeilToInt(.2f * globalMax);
            Texture3D tex = new Texture3D(globalMax + padding, globalMax + padding, globalMax + padding,
                TextureFormat.RGBA32, false);
            tex.SetPixels(Enumerable.Repeat(Color.clear, tex.width * tex.height * tex.depth).ToArray());
            foreach (var v in voxels)
            {
                tex.SetPixel(v.pos.x + padding / 2, v.pos.y + (int)(globalMax * _yOffset), v.pos.z + padding / 2,
                    v.color);
            }

            _texture.Save(tex, _name.Split('_')[0]);
            ;
        }
    }
}