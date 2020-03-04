using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animations : MonoBehaviour {
    public Animator anim;
    Vector3 PreviousFramePosition;  // Or whatever your initial position is
    float Speed = 0f;
      bool walk = false;
        bool stand = false;
    void Start () {
        anim = GetComponent<Animator> ();
      
    }

    // Update is called once per frame
    void Update () {
        
        if (Speed > 0.1 && walk == false)
        {
            anim.Play ("BasicMotions@Walk01", -1, 0f);
            walk = true;
            stand = false;
        }else if(Speed < 0.1 && stand == false){
        anim.Play ("BasicMotions@Idle01", -1, 0f);
        walk = false;
        stand = true;
        }
       
         float movementPerFrame = Vector3.Distance (PreviousFramePosition, transform.position) ;
     Speed = movementPerFrame / Time.deltaTime;
     PreviousFramePosition = PreviousFramePosition;
      anim.SetFloat ("Speed", Speed);
      PreviousFramePosition= transform.position;
    }
}