#if UNITY_EDITOR
using System.IO;
using System.Threading;
using UnityEngine;
using DefaultNamespace;
using NaughtyAttributes;
using Unity.EditorCoroutines.Editor;

public class LeniaJsonToTexAsset : MonoBehaviour
{
    [Header("Run settings")] private bool _cancel;
    [SerializeField] private Texture3DSO _textureSO;
    [SerializeField] private TextureSettings _settings;

    [SerializeField] private string _path;

    //[SerializeField] private TextureSettings _settings;
    [Header("Result")] [SerializeField] [ReadOnly]
    private Texture3D _texture;

    private EditorCoroutine _running;
    private CancellationTokenSource _toker;
    [SerializeField] LeniaParser parser = new();
    [SerializeField] private LeniaHolder _holder;
    public LeniaParser Parser => parser;

    [Button("Process the file at _path into 3DTextures")]
    private void Run()
    {
        if (_running == null)
        {
            _toker = new CancellationTokenSource();
            parser.Init(_settings, -1, 1);
            var streamReader = new StreamReader(File.OpenRead(_path));
            FindObjectsByType<PythonCaller>(FindObjectsSortMode.None)[0].ReadOutput(
                streamReader, _toker, () => streamReader.EndOfStream, s =>
                {
                    parser.NewBlock(s, out Texture3D texture);
                    if (texture != null)
                    {
                        _textureSO.Save(texture,
                            "FromJson-Pix" + _settings.pixelSize + "-" +
                            _settings.format.ToString());
                        _toker.Cancel();
                        Save();
                    }
                    //Debug.Log("Dims : " + parser.Lenia.DimensionsString);
                });
        }
        else
        {
            Debug.LogWarning(
                "A file processing is already running, wait for it to end or hit Cancel and you'll be able to run again after it automatically cleanly exited");
        }
    }

    [Button]
    private void Save()
    {
        _holder.lenia = parser.Lenia;
    }
    [Button("Cancel")]
    private void Cancel()
    {
        _cancel = true;
        _toker.Cancel();
    }
}

#endif