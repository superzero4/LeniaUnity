using UnityEngine;

public class NDBlur : KernelInfo
{
    public override float KernelValue(uint[] coords, float distanceToCenter)
    {
        return 1f;
    }
}