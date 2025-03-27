using System;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

[Serializable]
public class LeniaParser
{
    TextureSettings _settings;
    [SerializeField] private Lenia3D lenia;
    int depth;
    int step;
    private NumberFormatInfo formatInfo;

    public TextureSettings TextureSettings => _settings;

    public Lenia3D Lenia => lenia;
    StringBuilder nbBuffer = new StringBuilder();
    private int firstDepth;

    public void Init(TextureSettings settings, int firstDepth)
    {
        firstDepth = 0;
        this._settings = settings;
        step = -1;
        lenia = new();
        //First generation
        depth = firstDepth;
        this.firstDepth = firstDepth;
        if (firstDepth >= 1)
            lenia.generations.Add(new(_settings.size.x));
        formatInfo = new NumberFormatInfo();
        formatInfo.NumberDecimalSeparator = ".";
        nbBuffer = new StringBuilder();
    }

    public void NewBlock(string block)
    {
        NewBlock(block, out _);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="line"></param>
    /// <param name="texture">Not null only if we succesfully parsed a full generation</param>
    public void NewBlock(string block, [CanBeNull] out Texture3D texture)
    {
        //Debug.Log("New block in parser : " + block);
        texture = null;
        string filtered = "";
        float value;
        foreach (var character in block)
        {
            switch (character)
            {
                case '[':
                    depth++;
                    switch (depth)
                    {
                        case 1:
                            lenia.generations.Add(new(_settings.size.x));
                            step++;
                            break;
                        case 2:
                            lenia.generations[^1].Add(new(_settings.size.y));
                            //Debug.Log("Parsed...");
                            break;
                        case 3:
                            lenia.generations[^1][^1].Add(new(_settings.size.z));
                            break;
                    }

                    break;
                case ',':
                case ']':
                    if (character == ']')
                    {
                        depth--;
                        if (depth == firstDepth)
                        {
                            texture = SetTexture(step);
                            Debug.LogWarning("Parsed one generation");
                        }
                        //We closed a generation
                    }

                    if (nbBuffer.Length > 0)
                    {
                        filtered = nbBuffer.ToString();
                        if (float.TryParse(filtered, NumberStyles.Any, formatInfo, out value))
                            lenia.generations[^1][^1][^1].Add(value);
                        else if (filtered.ToLower().Contains("nan".ToLower()))
                            lenia.generations[^1][^1][^1].Add(-1f);
                        nbBuffer.Clear();
                    }

                    break;
                default:
                    if (char.IsDigit(character) || character == formatInfo.NumberDecimalSeparator[0] ||
                        character == 'n' || character == 'a')
                    {
                        nbBuffer.Append(character);
                    }
                    else
                    {
                        //Ignored characters
                    }

                    break;
            }
        }
    }

    private Texture3D SetTexture(int step)
    {
        var _pixelSize = _settings.pixelSize;
        var _format = _settings.format;
        var total = _settings.size.z;
        var pixelCount = total / _pixelSize;
        //Assert.IsTrue(total % _pixelSize == 0);
        var texture = new Texture3D(_settings.size.x, _settings.size.y, pixelCount,
            _format, false);
        Debug.LogWarning("Texture start");
        for (int x = 0; x < _settings.size.x; x++)
        {
            for (int y = 0; y < _settings.size.y; y++)
            {
                for (int z = 0; z < pixelCount; z++)
                {
                    var pixel = lenia.generations[step][x][y];
                    var offset = z * _pixelSize;
                    Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    if (_pixelSize == 1)
                    {
                        //Ramp from blue to red based on value representing life of a cell
                        color = new Color(pixel[offset], 0f, 1 - pixel[offset],
                            Mathf.Lerp(0.2f, 1f, pixel[offset]));
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

                    texture.SetPixel(x, y, z,
                        color);
                }
            }
        }

        texture.Apply();
        return texture;
    }
}