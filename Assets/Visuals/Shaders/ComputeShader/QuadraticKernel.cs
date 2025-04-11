using UnityEngine;

public class QuadraticKernel : KernelInfo
{
    public override float KernelValue(float distanceToCenter)
    {
        return distanceToCenter <= 1f ? Mathf.Pow(4 * distanceToCenter * (1 - distanceToCenter), 4) : 0;
    }
}