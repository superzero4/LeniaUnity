using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using System.Text;
using Unity.EditorCoroutines.Editor;

[Serializable]
public struct Lenia2D
{
    public List<List<List<List<float>>>> cells;
}

public class Lenia : MonoBehaviour
{
    [Header("Run settings")] [SerializeField]
    private bool _run;

    [SerializeField] private bool _cancel;
    [SerializeField] private bool _clearFlipbookOnRun;

    [Header("References")] [SerializeField]
    private MaterialPropertyFlipbook _flipbook;

    [SerializeField] private Texture3DSO _textureSO;

    [Header("Texture Settings")] [SerializeField]
    private TextureFormat _format;

    [SerializeField, Range(1, 4)] private int _pixelSize;
    [SerializeField] private string _path;
    [Header("Result")] [SerializeField] private Texture3D _texture;
    private EditorCoroutine _running;
    private StreamReader _reader;

    private void OnValidate()
    {
        if (_run)
        {
            _run = false;
            if (_running == null)
            {
                _running = EditorCoroutineUtility.StartCoroutine(ProcessFile(), this);
            }
        }
    }
    
    IEnumerator ProcessFile()
    {
        //Can be modified externally of this method to block the execution
        _cancel = false;
        if(_clearFlipbookOnRun)
            _flipbook.Textures.Clear();
        DirectoryInfo parent = new DirectoryInfo(Application.dataPath).Parent;
        _reader = new StreamReader(File.OpenRead(Path.Combine(parent.FullName, _path)));
        Lenia2D lenia = new Lenia2D();
        lenia.cells = new();
        int depth = -1;
        int step = -1;
        StringBuilder sb = new();
        string filtered = "";
        string line;
        float value;
        
        NumberFormatInfo formatInfo = new NumberFormatInfo();
        formatInfo.NumberDecimalSeparator = ",";
        
        while (_reader.Peek() >= 0 && !_cancel)
        {
            line = _reader.ReadLine();
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

                filtered = sb.ToString();
                int cnt1 = 0;
                while (cnt1 < filtered.Length && filtered[cnt1] == '[')
                {
                    depth++;
                    switch (depth)
                    {
                        case 1:
                            lenia.cells.Add(new());
                            step++;
                            if (step >= 1)
                                SetTexture(lenia, step - 1);
                            Debug.LogWarning("Parsed one generation");
                            yield return new WaitForEndOfFrame();
                            break;
                        case 2:
                            lenia.cells[^1].Add(new());
                            //Debug.Log("Parsed...");
                            break;
                        case 3:
                            //case 4:
                            lenia.cells[^1][^1].Add(new());
                            break;
                    }

                    cnt1++;
                }

                int cnt2 = 1;
                while (cnt2<filtered.Length-1 && filtered[^cnt2] == ']')
                {
                    depth--;
                    cnt2++;
                }
                
                filtered = filtered.Substring(cnt1, filtered.Length - (cnt1 + cnt2-1));
                if (float.TryParse(filtered, NumberStyles.Any, formatInfo, out value))
                    lenia.cells[^1][^1][^1].Add(value);
            }
        }
        Debug.LogWarning("Done");
        _reader.Close();
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
        _textureSO.SetTexture(_texture, "Gen" + step.ToString() + "-Pix" + _pixelSize + "-" + _format.ToString());
        _flipbook.Textures.Add(_texture);
        Debug.LogWarning("Texture set");
    }

    private void OnDestroy()
    {
        _reader?.Close();
    }
}