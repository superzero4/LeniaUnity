using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

public class PointCloudRendererSimple : MonoBehaviour
{
    [SerializeField] private Texture3D texture;
    [SerializeField] private Shader pointShader;
    [SerializeField] private Color pointTint = Color.white;
    [SerializeField, Range(0, 1f)] public float pointSize = 0.05f;

    private Material pointMaterial;
    private ComputeBuffer pointBuffer;


    [StructLayout(LayoutKind.Sequential)]
    struct PointData
    {
        public Vector3 position;
        public float color;
    }

    void Start()
    {
        pointMaterial = new Material(pointShader);
        SetTexture();

        // Create the material
    }

    private void SetTexture()
    {
        // Get the pixel array from the texture
        Color[] pixelArray = texture.GetPixels();
        PointData[] pointDataArray = new PointData[pixelArray.Length];
        // Create a ComputeBuffer with the same length as the pixel array
        pointBuffer = new ComputeBuffer(pixelArray.Length, sizeof(float) * 4);
        for (int i = 0; i < texture.width; i++)
        {
            for (int j = 0; j < texture.height; j++)
            {
                for (int k = 0; k < texture.depth; k++)
                {
                    int index = i * texture.depth * texture.height + j * texture.depth + k;
                    pointDataArray[index] = new PointData
                    {
                        position = new Vector3(i, j, k),
                        color = pixelArray[index].r
                    };
                }
            }
        }

        // Set the data of the ComputeBuffer using the pixel array
        pointBuffer.SetData(pointDataArray);
        UpdateView();
    }


    void OnRenderObject()
    {
        if (pointMaterial == null || pointBuffer == null) return;

        // Set shader properties
        pointMaterial.SetColor("_Tint", pointTint);
        pointMaterial.SetFloat("_PointSize", pointSize);
        
        pointMaterial.SetPass(0);
        //Draw the points
        Graphics.DrawProceduralNow(MeshTopology.Points, pointBuffer.count);
    }

    private void UpdateView()
    {
        pointMaterial.SetBuffer("_PointBuffer", pointBuffer);
        //pointMaterial.SetMatrix("_Transform", transform.worldToLocalMatrix);

    }

    void OnDestroy()
    {
        // Release the ComputeBuffer when it's no longer needed
        if (pointBuffer != null)
        {
            pointBuffer.Release();
        }
    }

    public void SetTexture(Texture3D texture3D)
    {
        texture = texture3D;
        SetTexture();
        UpdateView();
    }
}