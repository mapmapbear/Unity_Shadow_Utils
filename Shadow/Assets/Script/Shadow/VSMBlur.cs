using UnityEngine;
using System.Collections;

public class VSMBlur : MonoBehaviour
{
    private Shader castShadow;
    private Material castMaterial;
    [Range(1,5)]
    public int blur = 2;
    private bool isUpdateCamera = true;
    private ShadowManager shadowManager;

    private void Awake()
    {
        shadowManager = GameObject.FindObjectOfType<ShadowManager>();
        castShadow = Shader.Find("Shadow/Blur");
        if (castShadow != null && castShadow.isSupported == false)
        {
            enabled = false;
        }
    }

    private void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
    {
        for (int i = 0; i < blur; i++)
        {
            Graphics.Blit(sourceTexture, destTexture, material);
            Graphics.Blit(destTexture, sourceTexture, material);
        }
        Graphics.Blit(sourceTexture, destTexture, material);
    }

    private void OnDisable()
    {
        if (castMaterial != null)
        {
            Destroy(castMaterial);
        }
    }

    public bool IsUpdateCamera
    {
        get
        {
            return isUpdateCamera;
        }
        set
        {
            if (isUpdateCamera == false)
            {
                GetComponent<Camera>().enabled = false;
            }
            else
            {
                transform.localRotation = shadowManager._staticLight.transform.localRotation;
                GetComponent<Camera>().enabled = true;
                isUpdateCamera = false;
            }
            isUpdateCamera = value;
        }
    }

    public Material material
    {
        get
        {
            if (castMaterial == null)
            {
                castMaterial = new Material(castShadow);
            }
            return castMaterial;
        }
    }
}