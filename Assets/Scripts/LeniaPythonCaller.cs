using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace DefaultNamespace
{
    public class LeniaPythonCaller : MonoBehaviour
    {
        [Header("Saving the first texture outputed)")] [SerializeField]
        private string _saveName;

        [SerializeField] private Texture3DSO _textureSO;

        [Header("Call Python")] [SerializeField]
        private PythonCaller _pythonCaller;

        [SerializeField] private string folder = "External\\Lenia\\Python";
        [SerializeField] private string _filename = "LeniaND";

        [InfoBox("-d/--dim")] [SerializeField, Range(1, 6)]
        private int _dimensions = 3;

        [InfoBox("-s/--size")] [SerializeField, Range(-1, 10)]
        private int _matrixSize = 5;

        [InfoBox("-p/--pixel")] [SerializeField, Range(1, 10)]
        private int _sizeMult;

        [SerializeField] private PointCloudRendererSimple _renderer;
        [SerializeField] private LeniaParser parser;
        [SerializeField, ReadOnly] private bool _running;

        [Button]
        private void Stop()
        {
            _pythonCaller.Stop();
        }


        [Button]
        private void Execute()
        {
            Debug.ClearDeveloperConsole();
            List<PythonCaller.Argument> args = new();
            args.Add(("dim", _dimensions));
            if (_matrixSize > 0)
                args.Add(("s", _matrixSize));
            if (_sizeMult > 0)
                args.Add(("pixel", _sizeMult));
            _pythonCaller.Init(folder, _filename, args.ToArray());
            parser = new LeniaParser();
            //TODO fix this size and use it correctly
            var size = 0b1 << _matrixSize;
            parser.Init(new TextureSettings(TextureFormat.RFloat, 1, 128),1);
            _pythonCaller.CallPython((s) =>
            {
                _running = _pythonCaller.Responding;
                OnOutput(s);
                if (UnityEngine.Random.value < 0.1f)
                {
                    Debug.Log($"{parser.Lenia.DimensionsString}");
                }
            });
        }

        private void OnOutput(string arg0)
        {
            Debug.Log(arg0);
            parser.NewBlock(arg0, out var texture);
            if (texture != null)
            {
                if (Application.isPlaying)
                    _renderer.SetTexture(texture);
                if (_textureSO != null)
                {
                    _textureSO.Save(texture,
                        "InitState-Name" + _saveName + "-Pix" + parser.TextureSettings.pixelSize + "-" +
                        parser.TextureSettings.format.ToString());
                    //_textureSO = null; //We ensure we only save the first texture
                }


                Debug.Log("Generation fully parsed, Texture set");
            }
        }
    }
}