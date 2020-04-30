using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#region 업데이트 내역
/*
    O.1 : 최초 작성
        기본 리소스 파일들(음악, 팔레트 등)과 GreenOrb를 제외한 오브젝트 배치
        원작의 GreenOrb를 Item으로 대체하거나 하는 등의 작업이 필요해보임

*/
#endregion

public class PolygonCommand : MonoBehaviour
{
    #region 태그에 따른 호출


    void Start()
    {

    }


    void Update()
    {
        if (CompareTag("GameManager")) ;//Update_GM();
    }

    void Awake()
    {
        if (CompareTag("GameManager")) Awake_GM();
    }

    void FixedUpdate()
    {
        if (CompareTag("GameManager")) ;// FixedUpdate_GM();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {

    }

    void OnTriggerEnter2D(Collider2D collision)
    {

    }
    #endregion 태그에 따른 호출




    #region GameManger.Cs
    [Header("GameMangerValue")]
    public float centerY = -6f;
    public GameObject P_Ball, P_Item, P_Block, P_ParticleYellow;
    public GameObject BallPreview, Arrow, GameOverPanel, BallCountTextObj, BallPlusTextObj;
    public Transform GreenBallGroup, BlockGroup, BallGroup;
    public LineRenderer MouseLR, BallLR;
    public Text BestStageText, StageText, BallPowerText, FinalScoreText, NewRecordText;
    public Color[] blockColor;
    public Color greenColor;
    public AudioSource S_GameOver, S_Item, S_Plus;
    public AudioSource[] S_Block;
    public Quaternion QI = Quaternion.identity;
    public bool shotTrigger, shotable;
    public Vector3 veryFirstPos;

    Vector3 firstPos, secondPos, gap;
    int score, timerCount, launchIndex;
    bool timerStart, isDie, isNewRecord, isBlockMoving;
    float timeDelay;

    #region 시작
    void Awake_GM()
    {
        Camera cam = Camera.main;
        Rect rect = cam.rect;
        float scaleheight = ((float)Screen.width / Screen.height) / ((float)9 / 16);//가로/세로
        float scalewidth = 1f / scaleheight;
        if (scaleheight < 1)
        {
            rect.height = scaleheight;
            rect.y = (1f - scaleheight) / 2f;
        }
        else
        {
            rect.width = scalewidth;
            rect.x = (1f - scalewidth) / 2f;
        }
        cam.rect = rect;

        //시작
        BlockGenerator();
        BestStageText.text = "최고기록 : " + PlayerPrefs.GetInt("BestStage").ToString();
    }
    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void VeryFirstPosSet(Vector3 pos) { if (veryFirstPos == Vector3.zero) veryFirstPos = pos; }
    #endregion 시작

    #region 블럭
    void BlockGenerator()
    {

    }

    #endregion 블럭

    #endregion GameManger.Cs


}
