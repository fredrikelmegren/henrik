using UnityEngine;
using System.Collections;

[AddComponentMenu("Fog of War/FoWPro Shader")]
public class FoWPro : PostEffectsBase
{
    public Shader Shader;
    public Material Material;

    private bool _isInitialized = false;

    public override void OnEnable()
    {
        camera.depthTextureMode |= DepthTextureMode.Depth;

        base.OnEnable();
    }

    public override bool CheckResources()
    {
        CheckSupport(true);

        if (Material == null)
        {
            Material = new Material(Shader);
            Material.color = Color.white;
        }

        if (!isSupported)
            ReportAutoDisable();
        return isSupported;
    }

    public void SetValues(Texture2D texture, float minX, float minY, float maxX, float maxY)
    {
        Material.SetTexture("_FogTex", texture);

        Material.SetFloat("_minX", minX);
        Material.SetFloat("_maxX", maxX);
        Material.SetFloat("_minZ", minY);
        Material.SetFloat("_maxZ", maxY);

        _isInitialized = true;
    }

    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!_isInitialized)
        {
            Graphics.Blit(source, destination, Material);
            return;
        }

        var cameraNear = camera.nearClipPlane;
        var cameraFar = camera.farClipPlane;
        var cameraFov = camera.fieldOfView;
        var cameraAspectRatio = camera.aspect;

        var frustumCorners = Matrix4x4.identity;

        var fovWHalf = cameraFov * 0.5f;

        var toRight = camera.transform.right * cameraNear * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * cameraAspectRatio;
        var toTop = camera.transform.up * cameraNear * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

        var topLeft = (camera.transform.forward * cameraNear - toRight + toTop);
        var cameraScale = topLeft.magnitude * cameraFar / cameraNear;

        topLeft.Normalize();
        topLeft *= cameraScale;

        var topRight = (camera.transform.forward * cameraNear + toRight + toTop);
        topRight.Normalize();
        topRight *= cameraScale;

        var bottomRight = (camera.transform.forward * cameraNear + toRight - toTop);
        bottomRight.Normalize();
        bottomRight *= cameraScale;

        var bottomLeft = (camera.transform.forward * cameraNear - toRight - toTop);
        bottomLeft.Normalize();
        bottomLeft *= cameraScale;

        frustumCorners.SetRow(0, topLeft);
        frustumCorners.SetRow(1, topRight);
        frustumCorners.SetRow(2, bottomRight);
        frustumCorners.SetRow(3, bottomLeft);

        Material.SetMatrix("_FrustumCornersWS", frustumCorners);
        Material.SetVector("_CameraWS", camera.transform.position);

        //
        //

        //Graphics.Blit(source, destination, Material);

        RenderTexture.active = destination;
        Material.SetTexture("_MainTex", source);

        GL.PushMatrix();
        GL.LoadOrtho();

        Material.SetPass(0);

        GL.Begin(GL.QUADS);
        GL.TexCoord2(0, 0); GL.Vertex3(0, 0, 3f);
        GL.TexCoord2(1, 0); GL.Vertex3(1, 0, 2f);
        GL.TexCoord2(1, 1); GL.Vertex3(1, 1, 1f);
        GL.TexCoord2(0, 1); GL.Vertex3(0, 1, 0f);
        GL.End();
        GL.PopMatrix();
    }
}
