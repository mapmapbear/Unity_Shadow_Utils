using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowManager : MonoBehaviour
{
    public GameObject _dynamicLight; //光源
    public GameObject _staticLight; //平行光 -- 用于控制旋转平行光强制刷新静态阴影
    //public GameObject[] light;
    public float farClipPlane = 250; //远平面
    public float shadowMapSize = 2048; //ShadowMap分辨率
    public float varianceMapSize = 512;
    [SetProperty("VarianceCameraSize")] public float varianceCameraSize = 100;
    public LayerMask castLayerMasks; //投射阴影层
    public LayerMask receiveLayerMasks; //投射并接收阴影层
    public LayerMask globalStaticShadow;
    [SetProperty("PcfState")] public PCFState pcfState = PCFState.PCF_2X2; //PCF状态
    [SetProperty("ShadowEnable")] public bool _shadowEnable = true; //总阴影开关
    [SetProperty("StaticShadow")] public bool _staticShadow = true;
    [SetProperty("DynamicShadow")] public bool _dynamicShadow = true;

    private float _time = 10;
    private bool _isInit = true;
    private Shader shader; //替换光源相机渲染的shader
    private Shader castShadowVSM;
    private float distance; //主相机与方差相机的距离
    private Camera viewCamera; //视野相机
    private Camera lightCamera; //光源相机
    private Camera varianceCamera; //方差相机
    private Quaternion _lightRotation; //平行光旋转信息
    private RenderTexture shadowMapPCF; //RenderTexture
    private RenderTexture ordinaryMap;
    private Transform[] allGameObjects; //场景所有游戏物体
    private bool isDirectionalLight = true; //是否使用的是平行光
    private RenderTexture shadowMapVariance; //方差阴影贴图
    private List<Vector3> viewCameraFrustumVertexs = new List<Vector3>(); //视野相机视锥体顶点
    private List<GameObject> receiveShadowObjects = new List<GameObject>(); //接收阴影的物体

    private void LateUpdate()
    {
        _time -= Time.deltaTime;
        if (_dynamicLight != null)
        {
            if (_isInit)
            {
                _isInit = false;
                Init();
            }
            lightCamera.cullingMask = castLayerMasks; //更新光源相机要渲染哪些层
            varianceCamera.cullingMask = globalStaticShadow;
            ShadowUtils.MatrixToShader(lightCamera); //把光源相机矩阵传入Shader
            ShadowUtils.VarianceMatrixToShader(varianceCamera); //把方差相机矩阵传入Shader

            SpotLight spot = _dynamicLight.GetComponent<SpotLight>();
            if (spot != null)
            {
                lightCamera.fieldOfView = spot.SpotAngle;
            }
            
            if (_staticLight != null)
            {
                varianceCamera.transform.localRotation = _staticLight.transform.localRotation;

                if (_time <= 0)
                {
                    _time = 3;
                    varianceCamera.GetComponent<VSMBlur>().IsUpdateCamera = true;
                }

                if (distance >= varianceCameraSize/3 - 1 || _staticLight.transform.localRotation != _lightRotation)
                    //-1是为了第一次一定能进这个判断，精度原因
                {
                    _lightRotation = _staticLight.transform.localRotation;
                    if (distance >= varianceCameraSize/3 - 1)
                    {
                        varianceCamera.transform.localPosition = new Vector3(viewCamera.transform.position.x, 0,
                            viewCamera.transform.position.z);
                    }
                    varianceCamera.GetComponent<VSMBlur>().IsUpdateCamera = true;
                }
                else
                {
                    varianceCamera.GetComponent<VSMBlur>().IsUpdateCamera = false;
                }
                distance = Vector3.Distance(varianceCamera.transform.position,
                    new Vector3(viewCamera.transform.position.x, 0, viewCamera.transform.position.z));
            }
        }
    }

    private void FixedUpdate()
    {
        if (_dynamicLight != null)
        {
            if (isDirectionalLight == true) //是平行光
            {
                if (viewCamera == null || lightCamera == null)
                    return;
                viewCameraFrustumVertexs = ShadowUtils.GetPerspectiveCameraFrustumVertexs(viewCamera, farClipPlane);
                    //视野相机视锥体顶点
                ShadowUtils.OrthogonalLightCameraSelfAdaption(lightCamera, _dynamicLight, viewCameraFrustumVertexs,
                    shadowMapSize, farClipPlane); //光源正交相机自适应
            }
        }
    }

    public float VarianceCameraSize
    {
        get { return varianceCameraSize; }

        set
        {
            if (value == null)
            {
                return;
            }
            varianceCamera.orthographicSize = varianceCameraSize;
            varianceCamera.nearClipPlane = -varianceCameraSize; //用场景包围盒高度设置方差相机近平面
            varianceCamera.farClipPlane = varianceCameraSize; //用场景包围盒高度设置方差相机远平面
            varianceCameraSize = value;
        }
    }

    public bool DynamicShadow
    {
        get { return _dynamicShadow; }
        set
        {
            if (value)
                Shader.EnableKeyword("DynamicShadow");
            else
                Shader.DisableKeyword("DynamicShadow");
            _dynamicShadow = value;
        }
    }

    public bool ShadowEnable
    {
        get { return _shadowEnable; }

        set
        {
            if (value)
            {
                if (lightCamera == null)
                    return;
                else
                    lightCamera.enabled = true;
                Shader.EnableKeyword("ShadowEnable");
            }
            else
            {
                if (lightCamera == null)
                    return;
                else
                    lightCamera.enabled = false;
                Shader.DisableKeyword("ShadowEnable");
            }
            _shadowEnable = value;
        }
    }

    public PCFState PcfState
    {
        get { return pcfState; }
        set
        {
            if (value == PCFState.NO_PCF)
            {
                Shader.EnableKeyword("NO_PCF");
                Shader.DisableKeyword("PCF_2X2");
                Shader.DisableKeyword("PCF_4X4");
                Shader.DisableKeyword("PCF_8X8");
                Shader.DisableKeyword("PCF_16X16");
            }
            else if (value == PCFState.PCF_2X2)
            {
                Shader.DisableKeyword("NO_PCF");
                Shader.EnableKeyword("PCF_2X2");
                Shader.DisableKeyword("PCF_4X4");
                Shader.DisableKeyword("PCF_8X8");
                Shader.DisableKeyword("PCF_16X16");
            }
            else if (value == PCFState.PCF_4X4)
            {
                Shader.DisableKeyword("NO_PCF");
                Shader.DisableKeyword("PCF_2X2");
                Shader.EnableKeyword("PCF_4X4");
                Shader.DisableKeyword("PCF_8X8");
                Shader.DisableKeyword("PCF_16X16");
            }
            else if (value == PCFState.PCF_8X8)
            {
                Shader.DisableKeyword("NO_PCF");
                Shader.DisableKeyword("PCF_2X2");
                Shader.DisableKeyword("PCF_4X4");
                Shader.EnableKeyword("PCF_8X8");
                Shader.DisableKeyword("PCF_16X16");
            }
            else if (value == PCFState.PCF_16X16)
            {
                Shader.DisableKeyword("NO_PCF");
                Shader.DisableKeyword("PCF_2X2");
                Shader.DisableKeyword("PCF_4X4");
                Shader.DisableKeyword("PCF_8X8");
                Shader.EnableKeyword("PCF_16X16");
            }
            pcfState = value;
        }
    }

    public bool StaticShadow
    {
        get { return _staticShadow; }

        set
        {
            if (value)
            {
                Shader.EnableKeyword("StaticShadow");
            }
            else
            {
                Shader.DisableKeyword("StaticShadow");
            }
            _staticShadow = value;
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    private void Init()
    {
        viewCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>(); //主相机
        shader = Shader.Find("Shadow/CastShadow");
        castShadowVSM = Shader.Find("Shadow/CastShadowVSM");
        shadowMapPCF = new RenderTexture((int) shadowMapSize, (int) shadowMapSize, 24, RenderTextureFormat.Shadowmap);
            //RenderTexture
        shadowMapPCF.name = "ShadowMapPCF"; //ShadowMap更名

        shadowMapVariance = new RenderTexture((int) varianceMapSize, (int) varianceMapSize, 24, RenderTextureFormat.ARGB32);
            //RenderTexture
        shadowMapVariance.name = "ShadowMapVariance";

        ordinaryMap = new RenderTexture((int) shadowMapSize, (int) shadowMapSize, 24, RenderTextureFormat.Shadowmap);
        ordinaryMap.name = "OrdinaryMap";

        ShadowEnable = true; //总阴影开关默认为true
        PcfState = pcfState;
        if (lightCamera == null)
        {
            GameObject camera2 = new GameObject();
            varianceCamera = camera2.AddComponent<Camera>();
            varianceCamera.name = "VarianceCamera";
            varianceCamera.clearFlags = CameraClearFlags.SolidColor;
            varianceCamera.backgroundColor = Color.white;
            varianceCamera.orthographic = true;
            varianceCamera.orthographicSize = VarianceCameraSize;
            varianceCamera.nearClipPlane = -varianceCameraSize;
            varianceCamera.farClipPlane = varianceCameraSize;
            varianceCamera.transform.localPosition = Vector3.zero;
            varianceCamera.transform.localScale = Vector3.one;
            camera2.AddComponent<VSMBlur>();
            varianceCamera.targetTexture = shadowMapVariance; //给方差阴影相机赋RenderTexture
            ShadowUtils.RenderTextureVarianceToShader(varianceCamera, shadowMapVariance); //把RenderTexture传入Shader

            CreateCamera();
        }
        else
        {
            Debug.LogError("已有lightCamera");
        }
        ShadowUtils.RenderTextureToShader(lightCamera, shadowMapPCF); //把RenderTexture传入Shader

        //原Start
        ShadowEnable = true;
        StaticShadow = true;
        DynamicShadow = true;
        distance = varianceCameraSize/3;
        //AABBBounds = new GameObject();
        //AABBBounds.name = "SceneAABBBounds";                                                    //场景包围盒
        allGameObjects = GameObject.FindObjectsOfType<Transform>();
        ShadowUtils.FindAllGameObject(allGameObjects, receiveShadowObjects, receiveLayerMasks);
        varianceCamera.SetReplacementShader(castShadowVSM, "RenderType"); //用CastShadowVSM shader替换方差相机渲染
        lightCamera.SetReplacementShader(shader, "RenderType"); //用CastShadow shader替换光源相机渲染
    }

    private void CreateCamera()
    {
        if (_dynamicLight.GetComponent<SpotLight>() != null)
        {
            //说明是聚光灯 --- 生成透视相机
            isDirectionalLight = false;
            GameObject go = new GameObject();
            lightCamera = go.AddComponent<Camera>();
            lightCamera.name = "LightCamera";
            lightCamera.transform.SetParent(_dynamicLight.transform);
            lightCamera.transform.localPosition = Vector3.zero;
            lightCamera.transform.localRotation = Quaternion.Euler(0, 0, 0);
            lightCamera.transform.localScale = Vector3.one;
            lightCamera.clearFlags = CameraClearFlags.SolidColor;
            lightCamera.nearClipPlane = 5;
            lightCamera.backgroundColor = Color.black;
            Shader.EnableKeyword("SpotShadow"); //开启聚光灯宏
        }
        else
        {
            //生成正交相机
//            if (GameObject.FindObjectOfType<SpotLight>() != null)
//                GameObject.FindObjectOfType<SpotLight>().enabled = false;                       //关闭SpotLight脚本
            GameObject camera1 = new GameObject();
            lightCamera = camera1.AddComponent<Camera>();
            lightCamera.name = "LightCamera";
            lightCamera.transform.SetParent(_dynamicLight.transform);
            lightCamera.transform.localPosition = Vector3.zero;
            lightCamera.transform.localRotation = Quaternion.Euler(0, 0, 0);
            lightCamera.transform.localScale = Vector3.one;
            lightCamera.clearFlags = CameraClearFlags.SolidColor;
            lightCamera.backgroundColor = Color.black;
            lightCamera.orthographic = true;
            Shader.DisableKeyword("SpotShadow");
        }
    }
}