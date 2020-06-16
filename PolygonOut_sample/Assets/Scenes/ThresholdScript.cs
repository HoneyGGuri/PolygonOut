using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ThresholdScript : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        StartCoroutine(OnTriggerEnter2D_Threshold(collision));
    }

    [Header("ThresholdValue")]
    public AudioSource S_ItemBreak;
    public GameObject P_ParticleYellow;
    public Quaternion QI = Quaternion.identity;
    public PolygonCommand PC;
    ThresholdScript TC;
    

    IEnumerator OnTriggerEnter2D_Threshold(Collider2D collision)
    {
        //아이템과 공이 충돌시 스크립트를 작성해야함
        if (collision.gameObject.CompareTag("Item"))
        {
            yield return null;
            S_ItemBreak.Play();
            Destroy(collision.gameObject);
            Destroy(Instantiate(P_ParticleYellow, collision.transform.position, QI), 1);

            //파티클 바꿔야함(지금 블럭용)
            /*Destroy(Instantiate(PC.P_ParticleYellow, collision.transform.position, PC.QI), 1);
            
            
            while (true)
            {
                
            }*/
        }
        else if (collision.gameObject.CompareTag("Block"))
        {
            Destroy(collision.gameObject);
            GameObject die = GameObject.Find("GameManager") as GameObject;
            die.GetComponent<PolygonCommand>().Death();
            die.GetComponent<PolygonCommand>().GameOver();
            yield return null;
        }
    }
}
