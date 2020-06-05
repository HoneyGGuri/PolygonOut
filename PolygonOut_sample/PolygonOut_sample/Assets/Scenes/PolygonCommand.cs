using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
//using GooglePlayGames;

#region 업데이트 내역
/*
    O.1 : 최초 작성
        기본 리소스 파일들(음악, 팔레트 등)과 GreenOrb를 제외한 오브젝트 배치
        원작의 GreenOrb를 Item으로 대체하거나 하는 등의 작업이 필요해보임
    O.2 : 함수 스크립트 추가 작성 및 애니메이션 일부 수정
        메인 카메라의 Idle과 CloseUp의 로직이 잘 못된 것을 수정
        드래그시 일정 횟수만큼 공이 튕기고 원점으로 돌아오는 스크립트 추가
        점수 및 블록, 아이템 생성 관련 스크립트 수정 중
    S.1 : 블럭 부셔지는 코드 추가
    O.3 : 애니메이션, 디자인 부분 수정
        임계선 추가. 임계선과 블럭, 아이템 충돌 코드 작성 중
    O.4 : 블럭 파괴시 애니메이션, 파티클 관련 코드 수정
        공이 충돌을 끝내고 시작점으로 돌아갈 때에도 블럭과 아이템을 파괴하는 버그 수정
    O.5 : ThresholdScript.cs 추가(한계선 충돌 내용 적용)
    S.2 : Main 화면 flex
    O.6 : 일시정지 화면 추가 및 발사 관련 일부 버그 수정
    O.7 : 블럭 생성 위치 좌표 수정, 블럭 크기 및 모양 수정, P_Block 그룹화
        10스테이지마다 튕기는 횟수 증가 기능 추가

*/
#endregion

public class PolygonCommand : MonoBehaviour
{
    #region 태그에 따른 호출


    void Start()
    {   /*
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();
        */
        if (CompareTag("Ball")) Start_Ball();
    }


    void Update()
    {
        if (Application.platform == RuntimePlatform.Android && Input.GetKeyDown(KeyCode.Escape))
            isPause = !isPause;
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

    void OnApplicationQuit()
    {
        //((PlayGamesPlatform)Social.Active).SignOut();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (CompareTag("Ball")) StartCoroutine(OnCollisionEnter2D_Ball(collision));

    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (CompareTag("Ball")) StartCoroutine(OnTriggerEnter2D_Ball(collision));
    }
    #endregion 태그에 따른 호출




    #region GameManger.Cs
    [Header("GameManagerValue")]
    public float centerY = -5f;//시작점의 Y좌표
    public GameObject P_Ball, P_Item, P_ParticleYellow;//프리팹들
    public GameObject[] P_Block;
    public GameObject BallPreview, Arrow, GameOverPanel, MainPanel, BallPowerTextObj, Threshold, PausePanel, MenuButtonGroup, HelpPanel, HelpTextGroup;
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
    public Button pausebtn;

    Vector3 firstPos, secondPos, gap;
    int stage, timerCount, launchIndex;
    int shootPower = 10000;//발사 속도
    float decrease = 0.95f;//감속 배율
    int BounceCnt, CurCnt;//튕기는 횟수
    bool timerStart, isDie, isNewRecord, isBlockMoving, isReturn, isPause = false, onHelp = false;
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
        // BlockGenerator();
        BestStageText.text = "Best : " + PlayerPrefs.GetInt("BestStage").ToString();

    }

    public void on_start()
    {
        Social.localUser.Authenticate((bool success) =>
        {
            if (success) print("로그인 Id : " + Social.localUser.id);
            else print("실패");
        });
        BounceCnt = 10;
        MainPanel.SetActive(false);
        BlockGenerator();
    }

    //재시작버튼
    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void Pause()
    {
        if(shotable){ 
        isPause = !isPause;
        onHelp = false;
        }
    }
    public void Help() => onHelp = !onHelp;
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
        Application.OpenURL("http://google.com");
#else
        Application.Quit();
#endif
    }
    public void VeryFirstPosSet(Vector3 pos) { if (veryFirstPos == Vector3.zero) veryFirstPos = pos; }

    public void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Pause();
        }
        else
        {
            //Pause();
        }
    }

    public void OnApplicationFocus(bool focus)
    {
        
    }
    
    bool NowPlaying()
    {
        if (PausePanel.activeSelf == false && MainPanel.activeSelf == false)
            return true;
        else return false;
    }
    void Update_GM()
    {
        if (isDie) return;

        if (isPause && !onHelp)
        {
            BallPowerTextObj.SetActive(false);
            BestStageText.gameObject.SetActive(false);
            StageText.gameObject.SetActive(false);
            PausePanel.SetActive(true);
            MenuButtonGroup.SetActive(true);
            Time.timeScale = 0f;
        }
        else if(isPause && onHelp)
        {
            Time.timeScale = 0f;
            HelpPanel.SetActive(true);
            HelpTextGroup.SetActive(true);
            MenuButtonGroup.SetActive(false);

        }
        else
        {
            Time.timeScale = 1f;
            BallPowerTextObj.SetActive(true);
            BestStageText.gameObject.SetActive(true);
            StageText.gameObject.SetActive(true);
            MenuButtonGroup.SetActive(false);
            PausePanel.SetActive(false);
            HelpPanel.SetActive(false);
            HelpTextGroup.SetActive(false);
        }
        
        VeryFirstPosSet(new Vector3(0, -5f, 0));

        if (Input.GetMouseButtonDown(0) && NowPlaying())
        {//처음 터치했을 때 위치 계산
            print("First Input : " + Camera.main.ScreenToWorldPoint(Input.mousePosition));
            firstPos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0, 0, 10);//카메라 고려해서 Z+10
        }
        
        //공들이 움직이고 있다면 움직일 수 없다.
        shotable = true;
        for (int i = 0; i < BallGroup.childCount; i++)
            if (BallGroup.GetChild(i).GetComponent<PolygonCommand>().isMoving) shotable = false;
        if (isBlockMoving) shotable = false;

        //움직이는 중이면 Update_GM 실행 불가
        if (!shotable)
        {
            
            if (Input.GetMouseButton(0))
            {
                if (stage < 100) Time.timeScale = 3f;
                else Time.timeScale = 5;
            }
            else Time.timeScale = 1f;
            return;
        }
        
        
        if(shotTrigger)
        {
            BallPowerText.text = "x"+BounceCnt.ToString();
            shotTrigger = false;
            BlockGenerator();
            timeDelay = 0;
        }
        timeDelay += Time.deltaTime;
        if (timeDelay < 0.5f) return;//버그 방지용 딜레이

        bool isMouse = Input.GetMouseButton(0);
        if(isMouse && NowPlaying() && firstPos.y<64)//만약 계속 터치 중이면
        {
            secondPos =Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0, 0, 10);
            if ((secondPos - firstPos).magnitude < 3) return;//너무 가까우면(선을 그리기 작다면)
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
            BallLR.SetPosition(1, (Vector3)hit.point - gap * 2f);


        }
        //터치 중일때만 보임
        BallPreview.SetActive(isMouse && NowPlaying() && firstPos.y < 64);
        Arrow.SetActive(isMouse && NowPlaying() && firstPos.y < 64);

        if (Input.GetMouseButtonUp(0) && firstPos.y<64)
        {//라인 지우기
            secondPos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0, 0, 10);
            MouseLR.SetPosition(0, Vector3.zero);
            MouseLR.SetPosition(1, Vector3.zero);
            BallLR.SetPosition(0, Vector3.zero);
            BallLR.SetPosition(1, Vector3.zero);

            timerStart = true;

            //veryFirstPos
           
            
        }
    }
    void FixedUpdate_GM()
    {
        //아이템 먹어서 공이 여러 개면 일정 시간마다 발사
        if(timerStart && ++timerCount == 3)
        {
            timerCount = 0;
            print(firstPos + " / " + secondPos);
            if (firstPos == secondPos) return;
            BallGroup.GetChild(launchIndex++).GetComponent<PolygonCommand>().Launch(gap);
            if (launchIndex == BallGroup.childCount)
            {
                timerStart = false;
                launchIndex = 0;
                timerCount = 0;
            }
        }

    }


    #region 블럭
    void BlockGenerator()
    {
        print("BlockGenerator Start");
        StageText.text = "Stage " + (++stage).ToString();

        if (stage % 10 == 0 && stage<=100) BounceCnt += 2;
        else if(stage % 10 ==0 && stage>100) BounceCnt += 3;
        BallPowerText.text = "x" + BounceCnt.ToString();

        if(PlayerPrefs.GetInt("BestStage",0) < stage)
        {
            PlayerPrefs.SetInt("BestStage", stage);
            BestStageText.text = "Best : " + PlayerPrefs.GetInt("BestStage").ToString();
            BestStageText.color = greenColor;
            isNewRecord = true;

        }

        int count;
        int randBlock = Random.Range(0, 24);
        if (stage < 5) count = randBlock < 16 ? 2 : 3;
        else if (stage < 60) count = randBlock < 8 ? 2 : (randBlock < 16 ? 3 : 4);
        else if (stage < 120) count = randBlock < 8 ? 3 : (randBlock < 18 ? 4 : 5);
        else if (stage < 270) count = randBlock < 8 ? 4 : (randBlock < 20 ? 5 : 6);
        else count = randBlock < 10 ? 6 : (randBlock < 20 ? 7 : 8);

        List<Vector3> SpawnList = new List<Vector3>();
        //최대 한 번에 맵 밖에서 8개의 블럭이 나옴
        //6개는 위 아래로
        //수정 많이 필요!!!!!!!!!!!!!!
        for (int i = 0; i < 3; i++) SpawnList.Add(new Vector3(-50 + (i * 50), i%2==0?45 :69, 0));
        for (int i = 0; i < 3; i++) SpawnList.Add(new Vector3(-50+(i*50), i%2==0?-55:-79, 0));
        //7개 이상 나오면 2개는 양 옆으로
        if (count>=7)
            for (int i = 0; i < 2; i++) SpawnList.Add(new Vector3(-63+i*189,-5,0));

        for(int i = 0; i < count; i++)
        {
            int rand = Random.Range(0, SpawnList.Count);

            //스테이지별 블럭 등장
            Transform TR;
            
            if (stage<10)
                TR = Instantiate(P_Block[0], SpawnList[rand], QI).transform;
            else if(stage>=10 && stage <40){
                randBlock = Random.Range(0, 3);
                TR = Instantiate(randBlock<2?P_Block[0]:P_Block[1], SpawnList[rand], QI).transform;
            }
            else if(stage>=40 && stage<80){
                randBlock = Random.Range(0, 5);
                TR = Instantiate(randBlock < 2 ? P_Block[0] : randBlock < 4 ? P_Block[1] : P_Block[2], SpawnList[rand], QI).transform;
            }
            else if(stage>=80 && stage < 150)
            {
                randBlock = Random.Range(0, 10);
                TR = Instantiate(randBlock < 2 ? P_Block[0] : randBlock < 5 ? P_Block[1] : randBlock < 8? P_Block[2] : P_Block[3], SpawnList[rand], QI).transform;
            }
            else if(stage>=150 && stage < 300)
            {
                randBlock = Random.Range(0, 12);
                TR = Instantiate(randBlock < 2 ? P_Block[0] : randBlock < 5 ? P_Block[1] : randBlock < 8 ? P_Block[2] : randBlock<10? P_Block[3] : P_Block[4], SpawnList[rand], QI).transform;
            }
            else
            {
                randBlock = Random.Range(0, 14);
                TR = Instantiate(randBlock < 2 ? P_Block[0] : randBlock < 5 ? P_Block[1] : randBlock < 8 ? P_Block[2] : randBlock < 10 ? P_Block[3] : randBlock < 12? P_Block[4] : P_Block[5], SpawnList[rand], QI).transform;
            }
            TR.SetParent(BlockGroup);
            //블록 체력 부분
            int rand_2 = Random.Range(0,10);
            TR.GetChild(0).GetComponentInChildren<Text>().text = (stage < 20 ? (1) : (stage < 50 ? (rand_2 < 6 ? 1 : 2) : (rand_2 < 5 ? (stage / 25) - 1 : (rand_2 < 8 ? (stage / 25)  : (stage / 25)+1)))).ToString();
            /*
             * ~19 : 1
             * 20~49 : 1,2 (6:4 비율)
             * 50~ : 1,2,3 (5:3:2 비율)/ 25스테이지마다 +1
             * 
             */
           

            SpawnList.RemoveAt(rand);
        }
        //Instantiate(P_Item, SpawnList[Random.Range(0, SpawnList.Count)], QI).transform.SetParent(BlockGroup);
        isBlockMoving = true;
        print("Call MoveDown");
        for (int i = 0; i < BlockGroup.childCount; i++) StartCoroutine(BlockMoveDown(BlockGroup.GetChild(i)));

        
    }

    IEnumerator BlockMoveDown(Transform TR)
    {//수정 많이 필요!!!!!!!!!!!!!!
        yield return new WaitForSeconds(0.2f);
        Vector3 center=new Vector3(0,centerY,0);
        print("BlockMoveDown Start");
        Vector3 target=TR.position;
        if (TR.position.x > 0 && TR.position.y > -5) target = TR.position + new Vector3(-12,-12,0);//2시
        else if (TR.position.x < 0 && TR.position.y > -5) target = TR.position + new Vector3(12, -12, 0);//11시
        else if (TR.position.x == 0 && TR.position.y > -5) target = TR.position + new Vector3(0, -18, 0);//수직 위
        else if (TR.position.x > 0 && TR.position.y < -5) target = TR.position + new Vector3(-12, 12, 0);//5시
        else if (TR.position.x < 0 && TR.position.y < -5) target = TR.position + new Vector3(12, 12, 0);//7시
        else if (TR.position.x == 0 && TR.position.y < -5) target = TR.position + new Vector3(0, 18, 0);//수직 아래
        else if (TR.position.x > 0 && TR.position.y == -5) target = TR.position + new Vector3(-15, 0, 0);//오른쪽
        else if (TR.position.x < 0 && TR.position.y == -5) target = TR.position + new Vector3(15, 0, 0);//왼쪽
       
        
        /*
         *  Vector3 dir = (target - TR.position).magnitude;
        //블럭이 target 방향으로 회전하는 코드
        Vector3 dir = (TR.position-center);
        Vector3 target = new Vector3(dir.x, dir.y, dir.z);
        Quaternion angle = Quaternion.LookRotation(dir);
        //TR.rotation = Quaternion.Slerp(TR.rotation, angle, 0.1f);
        */

        float TT = 1.5f;
        while (true)
        {
            yield return null; TT -= Time.deltaTime;
            TR.position = Vector3.MoveTowards(TR.position, target, TT);//TR위치에서 target으로 TT 시간동안 이동
            if (TR.position == target) break;
        }

        isBlockMoving = false;
    }
    #endregion 시작

    #endregion 블럭

    public bool Death()
    {
        isDie = true;
        return isDie;
    }

    public void GameOver()
    {
        for (int i = 0; i < BallGroup.childCount; i++)
            Destroy(BallGroup.GetChild(i).gameObject);
        Destroy(Instantiate(P_ParticleYellow, veryFirstPos, QI), 1);

        BallPowerTextObj.SetActive(false);
        BestStageText.gameObject.SetActive(false);
        StageText.gameObject.SetActive(false);

        
        Camera.main.GetComponent<Animator>().SetTrigger("closeup");
        S_GameOver.Play();

        Invoke("PanelOn",1);


        return;
    }

    void PanelOn()
    {
        print("here");
        GameOverPanel.SetActive(true);
        FinalStageText.text = stage.ToString()+ " Stage Clear!";
        if (isNewRecord) NewRecordText.gameObject.SetActive(true);
    }



    #endregion GameManger.Cs


    #region BallScript.Cs
    [Header("BallScriptValue")]
    public Rigidbody2D RB;
    public bool isMoving;

    PolygonCommand PC;

    void Start_Ball() => PC = GameObject.FindWithTag("GameManager").GetComponent<PolygonCommand>();
    

    public void Launch(Vector3 pos)
    {
        if (pos.x + pos.y + pos.z == 0) return;
        isReturn = false;
        CurCnt = PC.BounceCnt;
        PC.shotTrigger = true;
        isMoving = true;
        print("CurCnt = "+CurCnt+" // BounceCnt :" +BounceCnt);
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
            if (CurCnt >= 0) BallPowerText.text = "x" + CurCnt.ToString();
            else BallPowerText.text = "x0";

            if (PC.BlockGroup.childCount == 0 && PC.ItemGroup.childCount == 0) CurCnt = 0;

            //너무 일찍 가속도가 내려가면 보정
            if (Mathf.Abs(RB.velocity.x) + Mathf.Abs(RB.velocity.y) < 70 && CurCnt>BounceCnt/2) RB.velocity = new Vector2(RB.velocity.x * 2, RB.velocity.y * 2);
            RB.velocity = new Vector2(RB.velocity.x * decrease, RB.velocity.y * decrease);

            if (CurCnt <= 0)
            {
                isReturn = true;
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
                        PC.S_Plus.Play();
                        yield break;
                    }
                }
                
            }
            
        }
        
        // 블럭충돌시 블럭숫자 1씩 줄어들다 0이되면 부숨
        if(Col.CompareTag("Block") && isReturn==false)
        {
            CurCnt--;
            if (CurCnt >= 0) BallPowerText.text = "x" + CurCnt.ToString();
            else BallPowerText.text = "x0";
            Text BlockText = Col.transform.GetChild(0).GetComponentInChildren<Text>();
            int blockValue = int.Parse(BlockText.text) - 1;



            for(int i = 0; i < PC.S_Block.Length; i++)
            {
                if ( PC.S_Block[i].isPlaying) continue;
                else { PC.S_Block[i].Play(); break; }
            }

            if(blockValue > 0)
            {
                BlockText.text = blockValue.ToString();
                Col.GetComponent<Animator>().SetTrigger("Shock");
            }
            else
            {
                Destroy(Col);
                Destroy(Instantiate(PC.P_ParticleYellow, collision.transform.position, QI), 1);
            }

            if (PC.BlockGroup.childCount == 0 && PC.ItemGroup.childCount == 0) CurCnt = 0;


            if (CurCnt <= 0)
            {
                isReturn = true;
                RB.velocity = new Vector2(RB.velocity.x * 0.2f, RB.velocity.y * 0.2f);
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
                        PC.S_Plus.Play();
                        yield break;
                    }
                }

            }
        }
    }

    IEnumerator OnTriggerEnter2D_Ball(Collider2D collision)
    {
        //아이템과 공이 충돌시 스크립트를 작성해야함
        if (collision.gameObject.CompareTag("Item") && isReturn==false)
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






#endregion BallScript.Cs

}
