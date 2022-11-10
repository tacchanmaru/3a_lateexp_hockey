using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputerPlayer : MonoBehaviour
{
    public Rigidbody rb;
    private string computer_level;
    private string prev_computer_level;
    private int InputSize = 6;
    private int HiddenSize = 12;
    private int HiddenLayers = 1;
    private int OutputSize = 2;

    private NNBrain brain;
    private HockeyAgent agent;

    void Awake() {
        // Debug.Log("hoge");
        brain = new NNBrain(InputSize, HiddenSize, HiddenLayers, OutputSize);
        agent = GetComponent<HockeyAgent>();
        computer_level = "EASY";
        string brain_txt = System.IO.Path.Combine(Application.streamingAssetsPath, "ComputerBrains/"+computer_level+".txt");
        brain.Load(brain_txt);
        prev_computer_level = computer_level;
    }


    void SetComputerLevel(string next_level) {
        computer_level = next_level;
    }

    //敵エージェントのレベルをEasyにセット
    public void SetEasyBrain() {
        Debug.Log("EASY");
        SetComputerLevel("EASY");
    }
    //敵エージェントのレベルをMediumにセット
    public void SetMediumBrain() {
        Debug.Log("MEDIUM");
        SetComputerLevel("MEDIUM");
    }
    //敵エージェントのレベルをHardにセット
    public void SetHardBrain() {
        Debug.Log("HARD");
        SetComputerLevel("HARD");
    }
    //敵エージェントのレベルをDEにセット
    public void SetDEBrain() {
        Debug.Log("DEBest");
        SetComputerLevel("DEBest");
    }
    public void SetNEBestBrain() {
        Debug.Log("NEBest");
        SetComputerLevel("NEBest");
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        if (!this.gameObject.activeSelf) {
            return;
        }
        //レベルに変更がある場合には、そのBrainをLoadする
        if (prev_computer_level != computer_level) {
            string brain_txt = System.IO.Path.Combine(Application.streamingAssetsPath, "ComputerBrains/"+computer_level+".txt");
            brain.Load(brain_txt);
        }
        
        //現在の状態を取得
        var observation = agent.CollectObservations();
        //行動を決定
        var action = brain.GetAction(observation);
        //行動の実施
        agent.AgentAction(action);

        agent.BattleTime = 0;
        if (agent.TimeUp) {
            agent.AgentReset();
        }

        prev_computer_level = computer_level;
    }

    //対戦モードに入るときに外から呼ばれる関数
    public void Activate() {
        this.gameObject.SetActive(true);
    }
    //対戦モードから出るときに外から呼ばれる関数
    public void Inactivate() {
        this.gameObject.SetActive(false);
    }
}
