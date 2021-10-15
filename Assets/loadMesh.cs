using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dummiesman;
using System.IO;
using UnityEngine;

public class loadMesh : MonoBehaviour
{
    // Start is called before the first frame update
    public string objPath = string.Empty;
    public GameObject loadedObject;
    void Start(){
            if (!File.Exists(objPath))
            {
                Debug.Log("File doesn't exist.");
            }else{
                if(loadedObject != null)            
                    Destroy(loadedObject);
                loadedObject = new OBJLoader().Load(objPath);
            }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


       