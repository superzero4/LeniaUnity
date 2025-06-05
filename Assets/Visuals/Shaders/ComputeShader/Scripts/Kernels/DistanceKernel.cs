namespace Visuals.Shaders.ComputeShader.Scripts.ComputeShader.Scripts
{
    public class DistanceKernel : KernelInfo
    {
        public override float KernelValue(uint[] coords, float distanceToCenter)
        {
            return distanceToCenter;
        }
    }
}