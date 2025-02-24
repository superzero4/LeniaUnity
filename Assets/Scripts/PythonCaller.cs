using System.Collections;
using System.Diagnostics;
using System.IO;
using NaughtyAttributes;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DefaultNamespace
{
    public class PythonCaller : MonoBehaviour
    {
        [SerializeField] private string folder = "External/Lenia/Python";
        [SerializeField] private string _filename = "LeniaND";
        [SerializeField] private string[] _args;

        [SerializeReference, ReadOnly] private Process _process;
        [SerializeReference, ReadOnly] private StreamReader _output;
        private EditorCoroutine _running;
        [Button]
        public void CallPython()
        {
            EditorCoroutineUtility.StopCoroutine(_running);
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = string.Join(folder + "/" + _filename + ".py", _args, ' '),
                    UseShellExecute = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            _running = EditorCoroutineUtility.StartCoroutine(ReadOutput(), this);
            _process.Start();
        }

        private IEnumerator ReadOutput()
        {
            yield return new WaitUntil(() => _process != null && _process.Associated  && !_process.HasExited && _process.Responding);
            _output = _process.StandardOutput;
            while (_process != null && !_process.HasExited && _process.Responding)
            {
                if (_output.Peek() > -1)
                    Debug.Log(_output.ReadLine());
                yield return new WaitForEndOfFrame();
            }
        }
    }
}