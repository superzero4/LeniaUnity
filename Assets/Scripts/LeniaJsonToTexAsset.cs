using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using System.Xml;
using NaughtyAttributes;
using Unity.EditorCoroutines.Editor;

public class LeniaJsonToTexAsset : MonoBehaviour
{
    [Header("Run settings")] private bool _cancel;
    [SerializeField] private Texture3DSO _textureSO;
    [SerializeField] private string _path;
    [SerializeField] private TextureSettings _settings;
    [Header("Result")] 
    [SerializeField] [ReadOnly] private Texture3D _texture;
    private EditorCoroutine _running;
    private StreamReader _reader;

    [Button("Process the file at _path into 3DTextures")]
    private void Run()
    {
        if (_running == null)
        {
            _running = EditorCoroutineUtility.StartCoroutine(ProcessFile(), this);
        }
        else
        {
            Debug.LogWarning(
                "A file processing is already running, wait for it to end or hit Cancel and you'll be able to run again after it automatically cleanly exited");
        }
    }

    [Button("Cancel")]
    private void Cancel()
    {
        _cancel = true;
    }

    IEnumerator ProcessFile()
    {
        //Can be modified externally of this method to block the execution
        _cancel = false;
        DirectoryInfo parent = new DirectoryInfo(Application.dataPath).Parent;
        _reader = new StreamReader(File.OpenRead(Path.Combine(parent.FullName, _path)));
        
        string line;
        LeniaParser parser = new();
        parser.Init(_settings);

        int step = 0;
        while (_reader.Peek() >= 0 && !_cancel)
        {
            line = _reader.ReadLine();
            parser.NewLine(line, out Texture3D texture);
            if (texture)
            {
                _textureSO.Save(texture,
                    "Gen" + step.ToString() + "-Pix" + _settings.pixelSize + "-" + _settings.format.ToString());
                step++;
                _texture = texture;
                yield return new WaitForEndOfFrame();
            }
        }

        Debug.LogWarning("Done");
        _reader.Close();
    }

    private void OnDestroy()
    {
        _reader?.Close();
    }
}