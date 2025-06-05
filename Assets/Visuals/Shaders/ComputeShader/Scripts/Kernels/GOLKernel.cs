using UnityEngine;

namespace Visuals.Shaders.ComputeShader.Scripts.ComputeShader.Scripts
{
    public class LLLKernel : KernelInfo
    {

        public override float KernelValue(uint[] coords, float relativeDistanceToCenter)
        {
            return 1f;
        }

        public override bool Normalize => false;
    }
}