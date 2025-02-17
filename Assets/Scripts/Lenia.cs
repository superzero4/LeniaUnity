using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using System.Text;
using DefaultNamespace;
using UnityEditor;
using UnityEngine.Assertions;

[Serializable]
public struct Lenia2D
{
    [SerializeField] public List<List<List<List<float>>>> cells;
}

public class Lenia : MonoBehaviour
{
    [SerializeField] private TextureFormat _format;
    [SerializeField, Range(1, 4)] private int _pixelSize;
    [SerializeField] private string _path;
    [SerializeField] private Texture3DSO _textureSO;
    [SerializeField] private Texture3D _texture;
    private StreamReader _reader;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        DirectoryInfo parent = new DirectoryInfo(Application.dataPath).Parent;
        _reader = new StreamReader(File.OpenRead(Path.Combine(parent.FullName, _path)));
        Lenia2D lenia = new Lenia2D();
        lenia.cells = new();
        int depth = -1;
        int step = -1;
        StringBuilder sb = new();
        while (_reader.Peek() >= 0)
        {
            var line = _reader.ReadLine();
            foreach (var str in line.Split(','))
            {
                sb.Clear();
                foreach (var ch in str)
                {
                    if (ch == '[' || ch == ']' || ch == '.' || char.IsDigit(ch))
                    {
                        sb.Append(ch);
                    }
                }

                string filtered = sb.ToString();
                while (filtered.Length != 0 && filtered[0] == '[')
                {
                    depth++;
                    switch (depth)
                    {
                        case 1:
                            lenia.cells.Add(new());
                            step++;
                            if (step >= 1)
                                SetTexture(lenia, step - 1);
                            Debug.LogWarning("Parsed..");
                            break;
                        case 2:
                            lenia.cells[^1].Add(new());
                            Debug.Log("Parsed...");
                            break;
                        case 3:
                            //case 4:
                            lenia.cells[^1][^1].Add(new());
                            yield return new WaitForEndOfFrame();
                            break;
                    }

                    filtered = filtered.Remove(0, 1);
                }


                if (float.TryParse(filtered, out var f))
                {
                    lenia.cells[^1][^1][^1].Add(f);
                }

                while (filtered.Length != 0 && filtered[^1] == ']')
                {
                    depth--;
                    filtered = filtered.Remove(filtered.Length - 1);
                }
            }
        }

        Debug.LogWarning("Done");
    }

    private void SetTexture(Lenia2D lenia, int step)
    {
        var total = lenia.cells[0][0][0].Count;
        var pixelCount = total / _pixelSize;
        //Assert.IsTrue(total % _pixelSize == 0);
        _texture = new Texture3D(lenia.cells[0].Count, lenia.cells[0][0].Count, pixelCount,
            _format, false);
        Debug.LogWarning("Texture start");
        for (int x = 0; x < lenia.cells[0].Count; x++)
        {
            for (int y = 0; y < lenia.cells[0][0].Count; y++)
            {
                for (int z = 0; z < pixelCount; z++)
                {
                    var pixel = lenia.cells[step][x][y];
                    var offset = z * _pixelSize;
                    Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    if (_pixelSize == 1)
                    {
                        //Ramp from blue to red based on value representing life of a cell
                        color = new Color(pixel[offset], 0f, 1 - pixel[offset], Mathf.Lerp(0.2f, 1f, pixel[offset]));
                    }
                    else
                    {
                        color.r = pixel[offset];
                        if (_pixelSize >= 2)
                        {
                            color.g = pixel[offset + 1];
                            if (_pixelSize >= 3)
                            {
                                color.b = pixel[offset + 2];
                                if (_pixelSize == 4)
                                {
                                    color.a = pixel[offset + 3];
                                }
                            }
                        }
                    }

                    _texture.SetPixel(x, y, z,
                        color);
                }
            }
        }

        _texture.Apply();
        _textureSO.SetTexture(_texture, "Gen"+step.ToString()+"-Pix"+_pixelSize+"-"+_format.ToString());
        Debug.LogWarning("Texture set");
    }

    private void OnDestroy()
    {
        _reader?.Close();
    }
}