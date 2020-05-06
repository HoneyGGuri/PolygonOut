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
    O.2 : 함수 스크립트 추가 작성 및 애니메이션 일부 수정
        메인 카메라의 Idle과 CloseUp의 로직이 잘 못된 것을 수정
        드래그시 일정 횟수만큼 공이 튕기고 원점으로 돌아오는 스크립트 추가
        점수 및 블록, 아이템 생성 관련 스크립트 수정 중

*/
#endregion

public class PolygonCommand : MonoBehaviour
{
    #region 태그에 따른 호출


    void Start()
    {
        if (CompareTag("Ball")) Start_Ball();
    }


    void Update()
    {
        if (CompareTag("GameManager")) Update_GM();
    }

    void Awake()
    {
        if (CompareTag("GameManager")) Awake_GM();
    }

    void FixedUpdate()
    {
        if (CompareTag("GameManager")) FixedUpdate_GM();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (CompareTag("Ball")) StartCoroutine(OnCollisionEnter2D_Ball(collision));

    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (CompareTag("Ball")) StartCoroutine(OnTriggerEnter2D_Ball(collision));
        if (CompareTag("Threshold")) StartCoroutine(OnTriggerEnter2D_Threshold(collision));
    }
    #endregion 태그에 따른 호출




    #region GameManger.Cs
    [Header("GameManagerValue")]
    public float centerY = -5f;//시작점의 Y좌표
    public GameObject P_Ball, P_Item, P_Block, P_ParticleYellow;//프리팹들
    public GameObject BallPreview, Arrow, GameOverPanel, BallPowerTextObj;
    public Transform ItemGroup, BlockGroup, BallGroup;//그룹들은 Transform
    public LineRenderer MouseLR, BallLR;
    public Text BestStageText, StageText, BallPowerText, FinalStageText, NewRecordText;
    public Color[] blockColor;
    public Color greenColor;
    public AudioSource S_GameOver, S_Item, S_Plus;//Sound
    public AudioSource[] S_Block;//Sound
    public Quaternion QI = Quaternion.identity;
    public bool shotTrigger, shotable;
    public Vector3 veryFirstPos;

    Vector3 firstPos, secondPos, gap;
    int stage, timerCount, launchIndex;
    int shootPower=10000;//발사 속도
    float decrease = 0.95f;//감속 배율
    int BounceCnt = 10,CurCnt;//튕기는 횟수
    bool timerStart, isDie, isNewRecord, isBlockMoving;
    float timeDelay;

    #region 시작
    void Awake_GM()
    {
        //16:9 비율 맞추기
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

    //재시작버튼
    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    public void VeryFirstPosSet(Vector3 pos) { if (veryFirstPos == Vector3.zero) veryFirstPos = pos; }

    void Update_GM()
    {
        VeryFirstPosSet(new Vector3(0, -5f, 0));

        if (Input.GetMouseButtonDown(0))//처음 터치했을 때 위치 계산
            firstPos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0, 0, 10);//카메라 고려해서 Z+10
        
        //공들이 움직이고 있다면 움직일 수 없다.
        shotable = true;
        for (int i = 0; i < BallGroup.childCount; i++)
            if (BallGroup.GetChild(i).GetComponent<PolygonCommand>().isMoving) shotable = false;
        if (isBlockMoving) shotable = false;

        //움직이는 중이면 Update_GM 실행 불가
        if (!shotable) return;
        
        if(shotTrigger && shotable)
        {
            shotTrigger = false;
            BlockGenerator();
            timeDelay = 0;
        }
        timeDelay += Time.deltaTime;
        if (timeDelay < 0.1f) return;//버그 방지용 딜레이

        bool isMouse = Input.GetMouseButton(0);
        if(isMouse)//만약 계속 터치 중이면
        {
            secondPos =Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0, 0, 10);
            if ((secondPos - firstPos).magnitude < 1) return;//너무 가까우면(선을 그리기 작다면)
            gap = (secondPos - firstPos).normalized;

            //미리보기
            Arrow.transform.position = veryFirstPos;
            Arrow.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(gap.y, gap.x) * Mathf.Rad2Deg);
            BallPreview.transform.position = Physics2D.CircleCast(new Vector2(Mathf.Clamp(veryFirstPos.x, -52.2f, 52.2f), centerY), 2f, gap, 10000, 1 << LayerMask.NameToLayer("Wall") | 1 << LayerMask.NameToLayer("Block")).centroid;
            RaycastHit2D hit = Physics2D.Raycast(veryFirstPos, gap, 10000, 1 << LayerMask.NameToLayer("Wall"));

            //라인 그리기
            MouseLR.SetPosition(0, firstPos);
            MouseLR.SetPosition(1, secondPos);
            BallLR.SetPosition(0, veryFirstPos);
            BallLR.SetPosition(1, (Vector3)hit.point - gap * 1.5f);
        }
        //터치 중일때만 보임
        BallPreview.SetActive(isMouse);
        Arrow.SetActive(isMouse);

        if (Input.GetMouseButtonUp(0))
        {//라인 지우기
            MouseLR.SetPosition(0, Vector3.zero);
            MouseLR.SetPosition(1, Vector3.zero);
            BallLR.SetPosition(0, Vector3.zero);
            BallLR.SetPosition(1, Vector3.zero);

            timerStart = true;
            
            //veryFirstPos
            firstPos=Vector3.zero;
            
        }
    }
    void FixedUpdate_GM()
    {
        //아이템 먹어서 공이 여러 개면 일정 시간마다 발사
        if(timerStart && ++timerCount == 3)
        {
            timerCount = 0;
            BallGroup.GetChild(launchIndex++).GetComponent<PolygonCommand>().Launch(gap);
            if (launchIndex == BallGroup.childCount)
            {
                timerStart = false;
                launchIndex = 0;
                timerCount = 0;
            }
        }

    }

    #endregion 시작

    #region 블럭
    void BlockGenerator()
    {
        StageText.text = "스테이지 " + (++stage).ToString();
        if(PlayerPrefs.GetInt("BestStage",0) < stage)
        {
            PlayerPrefs.SetInt("BestStage", stage);
            BestStageText.text = "최고기록 : " + PlayerPrefs.GetInt("BestStage").ToString();
            BestStageText.color = greenColor;
            isNewRecord = true;

        }

        int count;
        int randBlock = Random.Range(0, 24);
        if (stage <= 20) count = randBlock < 16 ? 3 : 4;
        else if (stage <= 40) count = randBlock < 8 ? 3 : (randBlock < 16 ? 4 : 5);
        else if (stage <= 80) count = randBlock < 8 ? 4 : (randBlock < 18 ? 5 : 6);
        else if (stage <= 200) count = randBlock < 8 ? 5 : (randBlock < 20 ? 6 : 7);
        else count = randBlock < 10 ? 6 : (randBlock < 20 ? 7 : 8);

        List<Vector3> SpawnList = new List<Vector3>();
        //최대 한 번에 맵 밖에서 8개의 블럭이 나옴
        //6개는 위 아래로
        //수정 많이 필요!!!!!!!!!!!!!!
        for (int i = 0; i < 3; i++) SpawnList.Add(new Vector3(68 - i * 68, 68, 0));
        for (int i = 0; i < 3; i++) SpawnList.Add(new Vector3(68 - i * 68, -68, 0));
        //7개 이상 나오면 2개는 양 옆으로
        if (count>=7)
            for (int i = 0; i < 2; i++) SpawnList.Add(new Vector3(68-i*136,-5,0));

        for(int i = 0; i < count; i++)
        {
            int rand = Random.Range(0, SpawnList.Count);

            Transform TR = Instantiate(P_Block, SpawnList[rand], QI).transform;
            TR.SetParent(BlockGroup);
            TR.GetChild(0).GetComponentInChildren<Text>().text = (stage < 10 ? 1 : stage / 10).ToString();

            SpawnList.RemoveAt(rand);
        }
        Instantiate(P_Item, SpawnList[Random.Range(0, SpawnList.Count)], QI).transform.SetParent(BlockGroup);
        isBlockMoving = true;
        for (int i = 0; i < BlockGroup.childCount; i++) StartCoroutine(BlockMoveDown(BlockGroup.GetChild(i)));

        
    }

    IEnumerator BlockMoveDown(Transform TR)
    {//수정 많이 필요!!!!!!!!!!!!!!
        yield return new WaitForSeconds(0.2f);
        Vector3 target=Vector3.zero;
        if (TR.position.x > 0 && TR.position.y > -5) target = TR.position + new Vector3(-33,-20,0);
        else if (TR.position.x < 0 && TR.position.y > -5) target = TR.position + new Vector3(33, -20, 0);
        else if (TR.position.x == 0 && TR.position.y > -5) target = TR.position + new Vector3(0, -20, 0);
        else if (TR.position.x > 0 && TR.position.y < -5) target = TR.position + new Vector3(-33, 20, 0);
        else if (TR.position.x < 0 && TR.position.y < -5) target = TR.position + new Vector3(33, 20, 0);
        else if (TR.position.x == 0 && TR.position.y < -5) target = TR.position + new Vector3(0, 20, 0);
        else if (TR.position.x > 0 && TR.position.y == -5) target = TR.position + new Vector3(-33, 0, 0);
        else if (TR.position.x < 0 && TR.position.y == -5) target = TR.position + new Vector3(-33, 0, 0);
        

        float TT = 1.5f;
        while (true)
        {
            yield return null; TT -= Time.deltaTime * 1.5f;
            TR.position = Vector3.MoveTowards(TR.position, target, TT);
            TR.Rotate(new Vector3(0, 0, 3),Space.Self);
            TR.Rotate(new Vector3(0, 0, 2), Space.Self);
            TR.Rotate(new Vector3(0, 0, 1), Space.Self);
            if (TR.position == target) break;
        }
        isBlockMoving = false;
    }

    #endregion 블럭

    #endregion GameManger.Cs


    #region BallScript.Cs
    [Header("BallScriptValue")]
    public Rigidbody2D RB;
    public bool isMoving;

    PolygonCommand PC;

    void Start_Ball() => PC = GameObject.FindWithTag("GameManager").GetComponent<PolygonCommand>();

    public void Launch(Vector3 pos)
    {
        CurCnt = BounceCnt;
        PC.shotTrigger = true;
        isMoving = true;
        print("pos :" +pos.x+" / "+pos.y + " / " +pos.z);
        RB.AddForce(pos * shootPower);
    }

    //공이 충돌시 좌표를 저장하기 위한 함수
    IEnumerator OnCollisionEnter2D_Ball(Collision2D collision)
    {
        
        Physics2D.IgnoreLayerCollision(2, 2);
        GameObject Col = collision.gameObject;

        //벽만? 블럭도?
        if (Col.CompareTag("Wall"))
        {
            CurCnt--;
            print("vel = " + RB.velocity.x + " / " + RB.velocity.y + " | CurCnt = " + CurCnt);
            //너무 일찍 가속도가 내려가면 보정
            if (Mathf.Abs(RB.velocity.x) + Mathf.Abs(RB.velocity.y) < 70 && CurCnt>BounceCnt/2) RB.velocity = new Vector2(RB.velocity.x * 2, RB.velocity.y * 2);
            RB.velocity = new Vector2(RB.velocity.x * decrease, RB.velocity.y * decrease);

            if (CurCnt == 0)
            {
                RB.velocity=new Vector2(RB.velocity.x * 0.2f, RB.velocity.y * 0.2f);
                yield return new WaitForSeconds(0.3f);
                RB.velocity = Vector2.zero;
                yield return new WaitForSeconds(1f);

                while (true)
                {
                    //yield return new WaitForSeconds(1f);
                    yield return null;
                    transform.position = Vector3.MoveTowards(transform.position, PC.veryFirstPos, 3);

                    if (transform.position == PC.veryFirstPos)
                    {
                        isMoving = false;
                        yield break;
                    }
                }
                
            }
            
        }
    }

    IEnumerator OnTriggerEnter2D_Ball(Collider2D collision)
    {
        //아이템과 공이 충돌시 스크립트를 작성해야함
        if (collision.gameObject.CompareTag("Item"))
        {
            Destroy(collision.gameObject);
            //파티클 바꿔야함(지금 블럭용)
            Destroy(Instantiate(PC.P_ParticleYellow, collision.transform.position, PC.QI), 1);

            PC.S_Item.Play();
            Transform TR = Instantiate(P_Item, collision.transform.position, PC.QI).transform;
            TR.SetParent(GameObject.Find("ItemGroup").transform);
            Vector3 targetPos = new Vector3(TR.position.x, PC.centerY, 0);

            while (true)
            {
                yield return null;
                TR.position = Vector3.MoveTowards(TR.position, targetPos, 2.5f);
                if (TR.position == targetPos) yield break;
            }
        }
    }
    IEnumerator OnTriggerEnter2D_Threshold(Collider2D collision)
    {
        print("what");
        //아이템과 공이 충돌시 스크립트를 작성해야함
        if (collision.gameObject.CompareTag("Item"))
        {
            yield return null;
            Destroy(collision.gameObject);
            //파티클 바꿔야함(지금 블럭용)
            /*Destroy(Instantiate(PC.P_ParticleYellow, collision.transform.position, PC.QI), 1);
            
            
            while (true)
            {
                
            }*/
        }
        else if (collision.gameObject.CompareTag("Block"))
        {
            print("fuck");

            yield return null;
            
        }
    }


    #endregion BallScript.Cs

}
