using System;
using NaughtyAttributes;
using UnityEngine;

namespace DefaultNamespace
{
    public class LeniaPythonCaller : MonoBehaviour
    {
        [SerializeField] private PythonCaller _pythonCaller;
        [SerializeField] private string folder = "External\\Lenia\\Python";
        [SerializeField] private string _filename = "LeniaND";

        [InfoBox("-d/--dim")] [SerializeField, Range(1, 6)]
        private int _dimensions = 3;

        [InfoBox("-s/--size")] [SerializeField, Range(1, 10)]
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
            _pythonCaller.Init(folder, _filename, ("dim", _dimensions), ("s", _matrixSize),
                ("pixel", _sizeMult.ToString()));
            parser = new LeniaParser();
            //TODO fix this size and use it correctly
            var size = 0b1 << _matrixSize;
            parser.Init(new TextureSettings(TextureFormat.RFloat, 1, size));
            _pythonCaller.CallPython((s) =>
            {
                _running = _pythonCaller.Responding;
                //Debug.Log(s);
                OnOutput(s);
                if (UnityEngine.Random.value < 0.1f)
                {
                    
                }
            });
        }

        private void OnOutput(string arg0)
        {
            parser.NewLine(arg0, out var texture);
            if (texture != null)
            {
                if (Application.isPlaying)
                    _renderer.SetTexture(texture);
                Debug.Log("Generation fully parsed, Texture set");
            }
        }
    }
}