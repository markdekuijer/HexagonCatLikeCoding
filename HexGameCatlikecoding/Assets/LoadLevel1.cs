using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadLevel1 : MonoBehaviour
{
    public void LoadLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }
}
