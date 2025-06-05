using UnityEngine;
using Visuals.Shaders.ComputeShader.Scripts;

public class NDBlur : KernelInfo
{

    public override float KernelValue(uint[] coords, float distanceToCenter)
    {
        return 1f;
    }
}