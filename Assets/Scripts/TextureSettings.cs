using UnityEngine;

[System.Serializable]
public struct TextureSettings
{
    [Header("Texture Settings")] [SerializeField]
    public TextureFormat format;

    [SerializeField, Range(1, 4)] public int pixelSize;
    [SerializeField] public Vector3Int size;
    public TextureSettings(TextureFormat format, int pixelSize, int size)
    {
        this.format = format;
        this.pixelSize = pixelSize;
        this.size = new Vector3Int(size, size, size);
    }
}