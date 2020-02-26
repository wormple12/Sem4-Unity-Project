using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Patrol : MonoBehaviour{

   public float speed;
   public float waitTime;
   public float startWaitTime;

   public Transform[] moveSpots;
   private int toSpot;

   void Start(){
       toSpot = 0;
   }

   void Update(){
 
       if(moveSpots.Length != 0){
       transform.position = Vector3.MoveTowards(transform.position, moveSpots[toSpot].position, speed * Time.deltaTime);
        
        if(Vector3.Distance(transform.position, moveSpots[toSpot].position) < 0.2f){
            if(waitTime <= 0){
                if(toSpot == moveSpots.Length-1){
                    toSpot = 0;
                }else{
                    toSpot = toSpot+1;
                }
                waitTime = startWaitTime;
            }else{
                waitTime -= Time.deltaTime;
            }
        }
   }
   }
}
