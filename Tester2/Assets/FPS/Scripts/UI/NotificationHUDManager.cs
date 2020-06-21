using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class NotificationHUDManager : MonoBehaviour
{
    [Tooltip("UI panel containing the layoutGroup for displaying notifications")]
    public RectTransform notificationPanel;
    [Tooltip("Prefab for the notifications")]
    public GameObject notificationPrefab;
    GameObject player;
    Health health { get; set; }
    
    void Awake()
    {
        PlayerWeaponsManager playerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, NotificationHUDManager>(playerWeaponsManager, this);
        playerWeaponsManager.onAddedWeapon += OnPickupWeapon;
        StartCoroutine(OnSceneLoad(SceneManager.GetActiveScene().name));
        /* Jetpack jetpack = FindObjectOfType<Jetpack>();
        DebugUtility.HandleErrorIfNullFindObject<Jetpack, NotificationHUDManager>(jetpack, this);
        jetpack.onUnlockJetpack += OnUnlockJetpack; */
    }
    void Start (){
        player = GameObject.FindWithTag ("Player");
		health = player.GetComponent<Health> ();
    }

    void Update (){

    }
    void OnUpdateObjective(UnityActionUpdateObjective updateObjective)
    {
        if (!string.IsNullOrEmpty(updateObjective.notificationText))
            CreateNotification(updateObjective.notificationText);
    }

    IEnumerator OnSceneLoad(string txt){
        if(txt == "Level1"){
            CreateNotification("Interact with objects with left mouse button");
             yield return new WaitForSeconds(10);
            CreateNotification("Blue walls are climable with left click");
             yield return new WaitForSeconds(10);
            CreateNotification("You can crouch by pressing 'c'");
             yield return new WaitForSeconds(10);
            CreateNotification("When you are a bird you can fly by pressing space repeatedly");
        }
        if(txt == "Level2"){
            CreateNotification("You can press 'e' to quickly telport forward");
             yield return new WaitForSeconds(10);
            CreateNotification("Some guards can be pickpocketet for money");
             yield return new WaitForSeconds(10);
            CreateNotification("You can sprint by pressing and holding 'shift'");
             yield return new WaitForSeconds(10);
            CreateNotification("Some levels require a set amount of money to progress");
        }
    }

    IEnumerator waiter(){

        yield return new WaitForSeconds(5);
    }

    void OnPickupWeapon(WeaponController weaponController, int index)
    {
        if (index != 0)
            CreateNotification("Picked up weapon : " + weaponController.weaponName);
    }

    /* void OnUnlockJetpack(bool unlock)
    {
        CreateNotification("Jetpack unlocked");
    } */

    public void CreateNotification(string text)
    {
        GameObject notificationInstance = Instantiate(notificationPrefab, notificationPanel);
        notificationInstance.transform.SetSiblingIndex(0);

        NotificationToast toast = notificationInstance.GetComponent<NotificationToast>();
        if (toast)
        {
            toast.Initialize(text);
        }
    }

    public void RegisterObjective(Objective objective)
    {
        objective.onUpdateObjective += OnUpdateObjective;
    }

    public void UnregisterObjective(Objective objective)
    {
        objective.onUpdateObjective -= OnUpdateObjective;
    }
}
