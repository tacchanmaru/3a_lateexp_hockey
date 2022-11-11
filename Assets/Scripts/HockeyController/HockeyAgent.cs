using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HockeyAgent : Agent
{
    // Playerの動かすオブジェクト、相手Playerのオブジェクト
    public GameObject Pack;
    public GameObject Opponent;
    
    // フィールドの上部にいるか、下部にいるか
    private int ModeSign;

    // 時間制限、現在の時間、時間切れ判定変数、ゲームの状態
    public float maxBattleTime = 40;
    public float BattleTime { get; set; }
    public bool TimeUp = false;
    public string gameState = "onPlaying";
    public int GoalCounter { get; set; }

    // パックに当たったか、経過後のカウント
    private bool HitPack = false;
    private int HitPackCounter = 10;
    

    // Playerコントローラ、パックのコントローラの取得
    private HockeyPlayer PlayerController;
    private PackManager PackManager;

    // 初期状態を保持しておくための変数
    private Vector3 StartPosition { get; set; }
    private Vector3 PackStartPosition { get; set; }
    private Vector3 StartInertia { get; set; }
    private Vector3 PackStartInertia { get; set; }

    // PackのVecocity取得用のRigidbody
    private Rigidbody pack_rb { get; set; }

    private void Awake() {
        // Playerの制御コントローラーを取得
        PlayerController = GetComponent<HockeyPlayer>();
        pack_rb = Pack.GetComponent<Rigidbody>();
        PackManager = Pack.GetComponent<PackManager>();
    }

    // 開始時に呼び出される初期化処理
    private void Start() {
        //初期位置の登録
        StartPosition = transform.position;
        PackStartPosition = Pack.transform.position;
        if (transform.position.z > 0) {
            ModeSign = -1;
        } else {
            ModeSign = 1;
        }
    }

    // 初期状態に戻す
    public override void AgentReset() {
        PlayerController.Reset();
        transform.position = StartPosition;
        BattleTime = 0;
        PackManager.ResetTime();
        TimeUp = false;
    }

    public override int GetState() {
        throw new NotImplementedException();
    }

    public override double[] ActionNumberToVectorAction(int ActionNumber) {
        throw new NotImplementedException();
    }

    // Agentへの入力を集める
    public override List<double> CollectObservations() {
        /*****
        入力を正規化することを考える.
        *****/
        var observations = new List<double>();
        double posScalingFactor = 10f;
        // double posScalingFactor = 2.0f;
        double velScalingFactor = 2f;
        // double velScalingFactor = 0.5f;
        var pos = transform.position;
        var pack_pos = Pack.transform.position;
        var opponent_pos = Opponent.transform.position;

        observations.Add(pos.x*posScalingFactor);
        observations.Add(pos.z*posScalingFactor*ModeSign);
        // observations.Add(pos.z*ModeSign);
        observations.Add((pos.x-pack_pos.x)*posScalingFactor);
        observations.Add((pos.z-pack_pos.z)*posScalingFactor*ModeSign);
        // observations.Add((pos.z-pack_pos.z)*ModeSign);
        // packの速度情報を入力
        observations.Add(pack_rb.velocity.x * velScalingFactor);
        observations.Add(pack_rb.velocity.z * velScalingFactor * ModeSign);
        // 相手のagentの位置を入力
        // observations.Add(opponent_pos.x*posScalingFactor);
        // observations.Add(opponent_pos.z*posScalingFactor*ModeSign);
        // observations.Add(opponent_pos.x*posScalingFactor);
        // observations.Add(opponent_pos.z*ModeSign);
        /*****
        NNのへの入力は-1~1程度に正規化すると良い
        ・weightsとbiasesを正規化して扱えるようにするため
        ・特定の入力が大きすぎるとそれに依存するから
         *****/
        return observations;
    }

    public override void Stop() {
        PlayerController.Stop();
    }
    
    // actionを受け取り、プレーヤーを動かし、報酬をセットする
    public override void AgentAction(double[] action) {
        // 時間切れなら何もしない
        if (TimeUp) { return; }
        // コントローラーにActionを渡す
        action[1] *= ModeSign;
        PlayerController.Move(action);
        
        // 時間を更新
        BattleTime += Time.fixedDeltaTime;

        // パックの正面にいればいるほど報酬を追加
        AddReward(1-Mathf.Abs(Pack.transform.position.x - transform.position.x));
       
        // ゴールを決めるとプラスの報酬
        if ((ModeSign == 1 && Pack.transform.position.z > 1.03f) || (ModeSign == -1 && Pack.transform.position.z < -1.03f) ) {
            GoalCounter++;
            AddReward(1000);
            AgentReset();
            TimeUp = true;
            gameState = "GetPoint";
            return;    
        }
        // ゴールを決められるとマイナスの報酬
        if ((ModeSign == 1 && Pack.transform.position.z < -1.03f) || (ModeSign == -1 && Pack.transform.position.z > 1.03f) ) {
            GoalCounter++;
            AddReward(-1000);
            AgentReset();
            TimeUp = true;
            gameState = "LosePoint";
            return;
        }

        // パックを押し出すことへの報酬
        if (HitPack) {
            AddReward(pack_rb.velocity.z * ModeSign * 10);
            HitPackCounter--;
        }
        if (HitPackCounter == 0) {
            HitPack = false;
            HitPackCounter = 10;
        }

        // 時間切れ判定
        if(BattleTime > maxBattleTime) {
            GoalCounter++;
            AgentReset();
            TimeUp = true;
            return;
        }

        // 試合終了判定
        if (GoalCounter >= 7) {
            AgentReset();
            TimeUp = true;
            Done();
            GoalCounter = 0;
            return;
        }
    }

    public void OnCollisionStay(Collision collision) {
        // 壁との衝突はマイナス報酬
        if (collision.gameObject.tag == "wall") {
            AddReward(-5);
        } else if (collision.gameObject.tag == "Pack") {
            HitPack = true;
        }
    }
}
