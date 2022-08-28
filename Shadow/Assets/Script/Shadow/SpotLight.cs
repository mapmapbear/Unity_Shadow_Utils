using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpotLight : MonoBehaviour
{
    [SetProperty("Range")]
    public float _range = 1000;                                                             //聚光灯范围
    [RangeAndSetProperty("SpotAngle", 1, 179)]
    public float _spotAngle = 60;                                                           //聚光灯角度
    public Color _spotColor = Color.white;                                                  //聚光灯颜色
    [SetProperty("Intensity")]
    public float _intensity = 1;                                                            //聚光灯强度
    [RangeAndSetProperty("Atten", -20, 20)]
    public float _atten = 1;

    public float Atten
    {
        get 
        {
            return _atten;
        }
        set
        {
            _atten = value; 
        }
    }

    private Camera lightCamera;

    private Vector3 pos;
    private Vector3 rot;

    public float Range
    {
        get { return _range; }
        set
        {
            if(lightCamera == null)
            {
                return;
            }
            lightCamera.farClipPlane = _range;
        }
    }

    public float SpotAngle
    {
        get { return _spotAngle; }
        set
        {
            if (lightCamera == null)
            {
                return;
            }
            lightCamera.fieldOfView = _spotAngle;
        }
    }

    public float Intensity
    {
        get { return _intensity; }
        set
        {
            if (lightCamera == null)
            {
                return;
            }
            _intensity = value;
        }
    }

    private void Start()
    {
        Shader.EnableKeyword("SpotLight");
    }

    private void OnDestroy()
    {
        Shader.DisableKeyword("SpotLight");
    }

    private void OnDisable()
    {
        Shader.DisableKeyword("SpotLight");
    }

    private void OnEnable()
    {
        Shader.EnableKeyword("SpotLight");
    }


    private void Update()
    {
        pos = this.gameObject.transform.position;
        rot = -this.gameObject.transform.forward;
        if (_range < 0)
        {
            _range = 0;
        }
        if (_intensity < 0)
        {
            _intensity = 0;
        }
        Shader.SetGlobalFloat("_SpotRange", _range);                                        //把聚光灯范围传入Shader
        Shader.SetGlobalFloat("_SpotAngle", _spotAngle);                                //把聚光灯角度传入Shader
        Shader.SetGlobalColor("_SpotColor", _spotColor);                                //把聚光灯颜色传入Shader
        Shader.SetGlobalFloat("_SpotIntensity", _intensity);                                //把聚光灯强度传入Shader
        Shader.SetGlobalVector("_SpotLightPos", new Vector4(pos.x, pos.y, pos.z, 1));   //把聚光灯位置传入Shader
        Shader.SetGlobalVector("_SpotLightRot", new Vector4(rot.x, rot.y, rot.z, 1));   //把聚光灯光方向传入Shader
        Shader.SetGlobalFloat("_Atten", Atten);
    }
}
