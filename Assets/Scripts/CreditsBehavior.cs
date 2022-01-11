using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsBehavior : MonoBehaviour
{

    public GameObject button;

    void Update() {
        if (Input.GetKeyDown(KeyCode.Mouse0)) { 
            TurnOnButton();
        }
    }

    public void TurnOnButton() { 
        button.SetActive(true);
    }

    public void GoToMainMenu() {
        SceneManager.LoadScene(0);
    }
}
