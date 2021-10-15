using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

// Go Free Asset in store : "Runtime OBJ Importer"
using Dummiesman;

public static class MatrixExtensions
{
    public static Quaternion ExtractRotation(this Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;
 
        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;
 
        return Quaternion.LookRotation(forward, upwards);
    }
 
    public static Vector3 ExtractPosition(this Matrix4x4 matrix)
    {
        Vector3 position;
        position.x = matrix.m03;
        position.y = matrix.m13;
        position.z = matrix.m23;
        return position;
    }
 
    public static Vector3 ExtractScale(this Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }
}

[Serializable]
public class CamConfig
{
    public float cx;
    public float cy;
    public float fx;
    public float fy;
    public float w;
    public float h;
    public string id;
    public List<float> extrinsic;
}
public class JsonHelper
{
    public static T[] getJsonArray<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>> (newJson);
        return wrapper.array;
    }
 
    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}
public class unityLoadMeshStructor : MonoBehaviour
{
    // file path
    public string root = "C:/Users/yushiang/Desktop/projects/OpenGL-viewer/app/2021-10-10-18-44-52-PlaneMeshes/";
    public string camsConfig = "camsConfig.json.json";
    public Shader replacementShader;
    public int guiOffset=0;
    // uniform
    private int useProject=0;

    // load data
    private int currentIndex=-1;
    private string[] subDirectories;
    private CamConfig[] camconfigs;

    CamConfig[] loadCamConfig(){     
        StreamReader file = new StreamReader(System.IO.Path.Combine(root, camsConfig));
        string loadJson = file.ReadToEnd();
        file.Close();
        return JsonHelper.getJsonArray<CamConfig> (loadJson);
    }

    string[] subFolderExample()
    {
        return Directory.GetDirectories(root);
    }

    Matrix4x4 getExtrinsicMat(CamConfig cam){
        // set instrinsic matrix
        Matrix4x4 extrinsic = new Matrix4x4();
        Vector4 row0 = new Vector4(
            cam.extrinsic[0], //rx
            cam.extrinsic[1], //ux
            cam.extrinsic[2], //lx
            cam.extrinsic[3]  //px
        );
        Vector4 row1 = new Vector4(
            cam.extrinsic[4], //ry
            cam.extrinsic[5], //uy
            cam.extrinsic[6], //ly
            cam.extrinsic[7] //py
        );
        Vector4 row2 = new Vector4(
            cam.extrinsic[8], //rz
            cam.extrinsic[9], //uz
            cam.extrinsic[10], //lz
            cam.extrinsic[11] //pz
        );
        Vector4 row3 = new Vector4(
            cam.extrinsic[12],
            cam.extrinsic[13],
            cam.extrinsic[14],
            cam.extrinsic[15]
        );


        extrinsic.SetRow(0,row0);
        extrinsic.SetRow(1,row1);
        extrinsic.SetRow(2,row2);
        extrinsic.SetRow(3,row3);

        return extrinsic;
    }

    Matrix4x4 getIntrinsicMat(CamConfig cam){
        // set world matrix
        Matrix4x4 instrinsic = new Matrix4x4();
        Vector4 row0 = new Vector4(
            cam.fx/cam.w, 
            0, 
            cam.cx/cam.w,
            0 
        );
        Vector4 row1 = new Vector4(
            0, 
            cam.fy/cam.h,
            cam.cy/cam.h,
            0 
        );
        Vector4 row2 = new Vector4(
            0,
            0,
            1,
            0
        );
        Vector4 row3 = new Vector4(
            cam.extrinsic[12],
            cam.extrinsic[13],
            cam.extrinsic[14],
            cam.extrinsic[15]
        );

        instrinsic.SetRow(0,row0);
        instrinsic.SetRow(1,row1);
        instrinsic.SetRow(2,row2);
        instrinsic.SetRow(3,row3);

        return instrinsic;
    }

    void setObj(int index){

        if(index<0 | index>= subDirectories.Length){
            Debug.LogError("Load Mesh error, index out of range");
            return;
        }

        string subFolder = subDirectories[index];
        // delete all children
        foreach (Transform child in transform) {
            GameObject.Destroy(child.gameObject);
        }

        foreach (CamConfig cam in camconfigs)
        {               
            string objPath = System.IO.Path.Combine(subFolder,cam.id+".obj");
            
            // load obj
            GameObject childObj = new OBJLoader().Load(objPath);
            Debug.Log(String.Format("<load obj> {0}",objPath));

            //set parent
            childObj.transform.parent = transform;
            
            // set instrinsic matrix
            Matrix4x4 instrinsic = getIntrinsicMat(cam);

            // replace material
            Renderer[] renderers = childObj.GetComponentsInChildren<Renderer>();
            foreach(Renderer renderer in renderers){
                renderer.material.shader = replacementShader;
                renderer.material.SetMatrix("_PinholeCamProjection", instrinsic);
                renderer.material.SetInt("_UseProject", useProject);
            }

            // set world matrix
            Matrix4x4 extrinsic = getExtrinsicMat(cam);
            childObj.transform.localScale = extrinsic.ExtractScale();
            childObj.transform.rotation = extrinsic.ExtractRotation();
            childObj.transform.position = extrinsic.ExtractPosition();
        }    
    }

    void OnGUI()
    {
        if(GUI.Button(new Rect(10+guiOffset*100, 10, 100, 100), String.Format("Toggle\n Project\nTexture\n {0}",useProject))){
            if(useProject==1){
                useProject=0;
            }else{
                useProject=1;
            }
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach(Renderer renderer in renderers){
                renderer.material.SetInt("_UseProject", useProject);
            }
        }
        if ((currentIndex+1 < subDirectories.Length))
        {
            if(GUI.Button(new Rect(10+guiOffset*100, 110, 100, 100), String.Format("Load\n Next {0}",currentIndex+1))){
                setObj(++currentIndex);
            }
        }

        if ((currentIndex-1 >= 0)){
            if(GUI.Button(new Rect(10+guiOffset*100, 210, 100, 100),  String.Format("Load\n Previous {0}",currentIndex-1))){
                setObj(--currentIndex);
            }
        }
    }

    void Start()
    {
        subDirectories = subFolderExample();
        camconfigs = loadCamConfig();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
