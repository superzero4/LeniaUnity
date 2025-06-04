using System.Collections;
using UnityEngine;

namespace Visuals.Shaders.ComputeShader.Scripts.Steps
{
    public class LifeStep : MonoBehaviour, IStep
    {
        [SerializeField, Range(0, 1f), Tooltip("0.1f")]
        private float _timeStep = 0.1f;

        [SerializeField, Range(0, 1f), Tooltip("0.12f")]
        private float _mu = 0.12f;

        [SerializeField, Range(0, .1f), Tooltip("0.10f")]
        private float _sigma = 0.01f;
        
        private static readonly int dt = Shader.PropertyToID("dt");
        private static readonly int mu = Shader.PropertyToID("mu");
        private static readonly int sigma = Shader.PropertyToID("sigma");
        private UnityEngine.ComputeShader _computeShader;
        public IEnumerator Step(float delay)
        {
            yield break;
        }

        public void Init(IInitValues init)
        {
            _computeShader.SetFloat(dt, _timeStep);
            _computeShader.SetFloat(mu, _mu);
            _computeShader.SetFloat(sigma, _sigma);
        }

        public void Release()
        {
            
        }
    }
}