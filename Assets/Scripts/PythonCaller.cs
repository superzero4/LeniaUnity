using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NaughtyAttributes;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace DefaultNamespace
{
    public class PythonCaller : MonoBehaviour
    {
        [InfoBox("Select the local correct interpretor/venv that has correct package installation")]
        [SerializeField]
        [CanBeNull]
        private string _pythonInterpreter = "python";

        [Header("Run settings"),
         InfoBox("Overridden when init is called externaly, use them for direct call from editor")]
        [SerializeField]
        [InfoBox("Working directory needs to be set correctly in order for the script to call it's dependecies")]
        private string folder = "External\\Lenia\\Python";

        [SerializeField] private string _filename = "LeniaND";
        [SerializeField] private Argument[] _args;
        public bool Responding => _process != null && !_process.HasExited;

        [Serializable]
        public struct Argument
        {
            private string id;
            private string value;

            public Argument(string id, string value)
            {
                this.id = id.Trim();
                this.value = value;
            }

            override public string ToString()
            {
                return (id.Length > 1 ? "--" : "-") + id + " " + value;
            }

            public static implicit operator Argument((string arg, object value) av)
            {
                return new Argument(av.arg, av.value.ToString());
            }
        }

        [SerializeReference, ReadOnly] private Process _process;
        private Task _running1;
        private Task _running2;
        private CancellationTokenSource _cancel = new();


        public void Init(string folder, string filename, params Argument[] args)
        {
            this.folder = folder;
            _filename = filename;
            _args = args;
        }

        //[Button]
        public void Stop()
        {
            if (_cancel != null && !_cancel.IsCancellationRequested)
                Debug.ClearDeveloperConsole();
            _cancel.Cancel();
        }

        [Button]
        private void CallPythonEditor()
        {
            CallPython(Debug.Log);
        }

        public void CallPython(Action<string> onOutput = null)
        {
            //Stop();
            string arguments = _filename + ".py" + " " + string.Join(' ', _args);
            var assets = Application.dataPath.Split('/');
            string workingDirectory = string.Join('\\', assets, 0, assets.Length - 1) + '\\' + folder;
            Debug.Log($" Executing python script {arguments} in {workingDirectory}");
            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonInterpreter,
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            _process = new Process() { StartInfo = startInfo };
            _process.Start();
            //Release if used
            //_process.MaxWorkingSet = new IntPtr(8000000000);
            //_process.MinWorkingSet = new IntPtr(2000000000);
            _process.PriorityClass = ProcessPriorityClass.High;
            _cancel = new CancellationTokenSource();
            _running1 = ReadOutput(_process.StandardError, Debug.LogWarning);
            _running2 = ReadOutput(_process.StandardOutput, onOutput);
        }

        public async Task ReadOutput(StreamReader stream, Action<string> onOutput = null)
        {
            try
            {
                while (!_cancel.IsCancellationRequested && _process != null && !_process.HasExited)
                {
                    var line = await stream.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                    {
                        onOutput?.Invoke(line);
                    }

                    await Task.Delay(160);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw e;
            }
            finally
        private IEnumerator ReadOutput()
        {
            //yield return new WaitUntil(() => _process != null && _process.Associated  && !_process.HasExited && _process.Responding);
            _output = _process.StandardOutput;
            while (_process != null && !_process.HasExited && _process.Responding)
            {
                onOutput?.Invoke("Process exited with code " + _process.ExitCode +
                                 " reading until the end of stream");
                if (!stream.EndOfStream)
                    onOutput?.Invoke(stream.ReadToEnd());
                stream.Close();
                stream.Dispose();
            }

            onOutput?.Invoke("Process exited with code " + _process.ExitCode +
                             " reading until the end of stream");
            if (!stream.EndOfStream)
                onOutput?.Invoke(stream.ReadToEnd());
            stream.Close();
            stream.Dispose();
        }
    }
}