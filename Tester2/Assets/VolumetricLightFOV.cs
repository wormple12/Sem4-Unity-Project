using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumetricLightFOV : MonoBehaviour {

	public Light FOVLight;
	[Tooltip ("Image component for the eye sprites")]
	public Image eyeImage;
	[Tooltip ("Sprite to display when seen")]
	public Sprite eyeOpenSprite;
	[Tooltip ("Sprite to display when unseen")]
	public Sprite eyeClosedSprite;

	public LayerMask targetMask;
	public LayerMask obstacleMask;

	public float damageModifier = 2f;
	public float healingCooldown = 2.0f;

	public List<Transform> visibleTargets = new List<Transform> ();

	GameObject player;
	Health health { get; set; }
	float viewRange;
	float viewAngle;
	float timeSinceSeen;

	void Start () {
		player = GameObject.FindWithTag ("Player");
		health = player.GetComponent<Health> ();
		viewRange = FOVLight.range;
		viewAngle = FOVLight.spotAngle;
		timeSinceSeen = healingCooldown;
		StartCoroutine ("FindTargetsWithDelay", .2f);
	}

	IEnumerator FindTargetsWithDelay (float delay) {
		while (true) {
			yield return new WaitForSeconds (delay);
			FindVisibleTargets ();
		}
	}

	void Update () {
		if (visibleTargets.Count > 0) {
			float dstToTarget = Vector3.Distance (transform.position, visibleTargets[0].position);
			float dstModifier = viewRange - dstToTarget + 1f;
			health.TakeDamage (dstModifier * damageModifier * 0.05f, this.gameObject);
			timeSinceSeen = 0f;
		} else if (timeSinceSeen < healingCooldown) {
			eyeImage.sprite = eyeClosedSprite;
			timeSinceSeen += Time.deltaTime;
		} else if (health.currentHealth < health.maxHealth) {
			health.Heal (0.1f);
		}
	}

	// alternative (maybe better performance):
	// collider trigger on enter; run only on player
	// if match; check Raycast in that direction
	void FindVisibleTargets () {
		visibleTargets.Clear ();
		Collider[] targetsInViewRadius = Physics.OverlapSphere (transform.position, viewRange, targetMask);

		for (int i = 0; i < targetsInViewRadius.Length; i++) {
			Transform target = targetsInViewRadius[i].transform;
			Vector3 dirToTarget = (target.position - transform.position).normalized;
			float dstToTarget = Vector3.Distance (transform.position, target.position);
			float angleBetweenForwardAndTarget = Vector3.Angle (transform.forward, dirToTarget);
			if (angleBetweenForwardAndTarget < viewAngle / 2 ||
				(dstToTarget < viewRange / 3 && angleBetweenForwardAndTarget < 90) // had to expand the perimeter when close to the target, otherwise it didn't catch it
			) {
				if (!Physics.Raycast (transform.position, dirToTarget, dstToTarget, obstacleMask)) {
					visibleTargets.Add (target);
					eyeImage.sprite = eyeOpenSprite;
				}
			}
		}
	}
}