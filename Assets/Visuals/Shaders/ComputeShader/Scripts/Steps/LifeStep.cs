using System.Collections;
using UnityEngine;

namespace Visuals.Shaders.ComputeShader.Scripts.Steps
{
    public class LifeStep : MonoBehaviour, IStep
    {
        private UnityEngine.ComputeShader _computeShader => ShaderCommons.GrowthKernel.shader;

        [Header("Settings")] [SerializeField, Range(0, 1f), Tooltip("0.1f")]
        private float _timeStep = 0.1f;

        [SerializeField, Range(0, 1f), Tooltip("0.12f")]
        private float _mu = 0.12f;

        [SerializeField, Range(0, .1f), Tooltip("0.10f")]
        private float _sigma = 0.01f;

        private static readonly int dt = Shader.PropertyToID("dt");
        private static readonly int mu = Shader.PropertyToID("mu");
        private static readonly int sigma = Shader.PropertyToID("sigma");
        private static readonly int midput = Shader.PropertyToID("_MidPut");
        private ComputeBuffer _result;
        private ComputeBuffer _last;
        
        public IEnumerator Step(ComputeBuffer buffer, float delay)
        {
            ShaderCommons.SetBuffers(ShaderCommons.GrowthKernel, _last, _result);
            ShaderCommons.SetBuffer(ShaderCommons.GrowthKernel, midput, buffer);
            LogBuffer(_last,"last");
            LogBuffer(buffer,"shared/midput");
            ShaderCommons.Dispatch(ShaderCommons.GrowthKernel, buffer.count);
            yield return new WaitForSeconds(delay);
            LogBuffer(_result,"result after growth dispatched");
            ShaderCommons.Copy(_result, buffer);
            ShaderCommons.Copy(_result, _last);
        }

        private void LogBuffer(ComputeBuffer buffer, string name = "buffer")
        {
            float[] data = new float[buffer.count];
            buffer.GetData(data);
            Debug.Log($"Buffer data: {name} {string.Join(", ", data)}");
        }

        public void Init(IInitValues init)
        {
            _computeShader.SetFloat(dt, _timeStep);
            _computeShader.SetFloat(mu, _mu);
            _computeShader.SetFloat(sigma, _sigma);

            _result = new ComputeBuffer(init.TotalSize, sizeof(float));
            var array = init.InitialValues();
            _last = new ComputeBuffer(array.Length, sizeof(float));
            _last.SetData(array);
        }

        public void Release()
        {
            _result?.Release();
            _last?.Release();
        }
    }
}