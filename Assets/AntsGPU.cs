using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AntsGPU : MonoBehaviour
{
    public ComputeShader shader;
    public int width = 1024;
    public int height = 1024;

    [Range(0.001f, 0.5f)] public float brushRadius = 0.05f;
    public Color brushColor = Color.red;

    RenderTexture rt;
    int kernel;
    uint tgx, tgy, tgz;
 
    Material targetMat;

    void Start()
    {
        // RenderTexture als UAV (schreibbar für Compute) anlegen
        rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        rt.enableRandomWrite = true;   // WICHTIG!
        rt.filterMode = FilterMode.Bilinear;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();

        kernel = shader.FindKernel("CSMain");
        shader.GetKernelThreadGroupSizes(kernel, out tgx, out tgy, out tgz);
        shader.SetTexture(kernel, "Result", rt);

        // Auf ein Mesh-Material binden (Alternative: UI RawImage.texture = rt)
        targetMat = GetComponent<Renderer>().material;
        targetMat.mainTexture = rt;

        // Optional: initial clear via Dispatch eines Clear-Kernels
        // Clear();
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            // Beispiel A: Malen über den Bildschirm (Fullscreen-Quad/Quad in 2D)
            Vector2 uv = new Vector2(
                Mathf.Clamp01(Input.mousePosition.x / Screen.width),
                Mathf.Clamp01(Input.mousePosition.y / Screen.height)
            );

            // Beispiel B (stattdessen): Wenn du auf ein Mesh malst, nutze Raycast und hit.textureCoord
            // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // if (Physics.Raycast(ray, out var hit)) uv = hit.textureCoord;

            PaintAt(uv);
        }
    }

    void PaintAt(Vector2 uv)
    {
        shader.SetVector("brushPos", new Vector4(uv.x, uv.y, 0, 0));
        shader.SetFloat("brushRadius", brushRadius);
        shader.SetVector("brushColor", (Vector4)brushColor);

        int groupsX = Mathf.CeilToInt(width  / (float)tgx);
        int groupsY = Mathf.CeilToInt(height / (float)tgy);
        shader.Dispatch(kernel, grou
