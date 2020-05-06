using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinnedMeshCollider : MonoBehaviour {
    // Start is called before the first frame update
    void Start () {
        meshRenderer = GetComponent<SkinnedMeshRenderer> ();
        meshCollider = GetComponent<MeshCollider> ();
    }

    private float time = 0;
    // Update is called once per frame
    void Update () {
        time += Time.deltaTime;
        if (time >= 0.5f) {
            time = 0;
            UpdateCollider ();
        }
    }

    SkinnedMeshRenderer meshRenderer;
    MeshCollider meshCollider;

    public void UpdateCollider () {
        Mesh colliderMesh = new Mesh ();
        meshRenderer.BakeMesh (colliderMesh);
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = colliderMesh;
    }
}