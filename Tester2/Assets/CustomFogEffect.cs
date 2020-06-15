using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CustomFogEffect : MonoBehaviour {

    public Shader deferredFog;

    [System.NonSerialized]
    Material fogMaterial;
    [System.NonSerialized]
    Camera deferredCamera;
    [System.NonSerialized]
    Vector3[] frustumCorners;
    [System.NonSerialized]
    Vector4[] vectorArray;

    [ImageEffectOpaque]
    void OnRenderImage (RenderTexture source, RenderTexture destination) {
        if (fogMaterial == null) {
            deferredCamera = GetComponent<Camera> ();
            frustumCorners = new Vector3[4];
            vectorArray = new Vector4[4];
            fogMaterial = new Material (deferredFog);
        }
        deferredCamera.CalculateFrustumCorners (
            new Rect (0f, 0f, 1f, 1f),
            deferredCamera.farClipPlane,
            deferredCamera.stereoActiveEye,
            frustumCorners
        );

        vectorArray[0] = frustumCorners[0];
        vectorArray[1] = frustumCorners[3];
        vectorArray[2] = frustumCorners[1];
        vectorArray[3] = frustumCorners[2];
        fogMaterial.SetVectorArray ("_FrustumCorners", vectorArray);

        Graphics.Blit (source, destination, fogMaterial);
    }

}