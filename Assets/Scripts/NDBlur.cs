using UnityEngine;

public class NDBlur : KernelInfo
{
    public override float KernelValue(uint[] coords, float distanceToCenter)
    {
        if (distanceToCenter > 1f)
            return 0f;
        for (int i = 0; i < coords.Length; i++)
            if (coords[i] > 1)
                return 0f;

        return 1f;
    }
}