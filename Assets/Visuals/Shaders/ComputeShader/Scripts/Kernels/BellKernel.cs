using UnityEngine;

namespace Visuals.Shaders.ComputeShader.Scripts.ComputeShader.Scripts
{
    public class BellKernel : KernelInfo
    {
        [SerializeField, Range(0, 1)] private float _rho = 0.5f;
        [SerializeField, Range(0, 1f)] private float _omega = .15f;

        public override float KernelValue(uint[] coords, float relativeDistanceToCenter)
        {
            return relativeDistanceToCenter<=1 ? bell(relativeDistanceToCenter, _rho, _omega) : 0f;
        }

        public static float bell(float x, float m, float s)
        {
            return Mathf.Exp(-Mathf.Pow(x-m, 2) / (2 * Mathf.Pow(s, 2)));
        }
    }
}