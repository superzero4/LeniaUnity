using UnityEngine;

namespace DefaultNamespace
{
    public class BufferFlipbook : TextureFlipbookBase
    {
        [SerializeField]
        private PointCloudRendererSimple _pcs;
        protected override void UpdateTexture(Texture3D texture3D)
        {
            _pcs.SetTexture(texture3D);
        }
    }
}