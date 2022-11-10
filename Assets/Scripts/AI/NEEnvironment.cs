using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using System.Text;

public class NEEnvironment : Environment
{
    /***** NE Parameters *****/
    [Header("Settings"), SerializeField] private int totalPopulation = 32;
    private int TotalPopulation { get { return totalPopulation; } }

    [SerializeField] private int tournamentSelection = 20;
    private int TournamentSelection { get { return tournamentSelection; } }

    [SerializeField] private int inputSize = 6;
    private int InputSize { get { return inputSize; } }

    [SerializeField] private int hiddenSize = 12;
    private int HiddenSize { get { return hiddenSize; } }

    [SerializeField] private int hiddenLayers = 1;
    private int HiddenLayers { get { return hiddenLayers; } }

    [SerializeField] private int outputSize = 2;
    private int OutputSize { get { return outputSize; } }

    [SerializeField] private int nAgents = 2;
    private int NAgents { get { return nAgents; } }

    [Header("Agent Prefab"), SerializeField] private GameObject GObject1;
    [Header("Agent Prefab"), SerializeField] private GameObject GObject2;
    [Header("UI References"), SerializeField] private Text populationText = null;
    private Text PopulationText { get { return populationText; } }

    /***** Values for Record *****/
    private float GenBestRecord { get; set; }
    private float GenSumReward { get; set; }
    private float GenAvgReward { get; set; }
    private float BestRecord { get; set; }

    private List<NNBrain> Brains { get; set; } = new List<NNBrain>();
    // NEでも親、子あるが、子はGenPopulationで作り、使い捨てる
    private List<NNBrain> WinnerBrains { get; set; } = new List<NNBrain>();
    // tournament用
    private Queue<NNBrain> CurrentBrains { get; set; }
    // SetStartAgentsでBrainsをQueueに変えるよう
    private List<GameObject> GObjects { get; } = new List<GameObject>();
    private List<HockeyAgent> Agents { get; } = new List<HockeyAgent>();
    private int Generation { get; set; }
    private List<AgentPair> AgentsSet { get; } = new List<AgentPair>();
    

    // flag
    public bool WaitingFlag = false; 
    // agent交代
    public bool RestartFlag = false;
    // goal,時間切れ
    public bool ManualModeFlag = false;

    private string GenerationFile = "./Assets/PreBrains/NEBrain/Generation.txt";
    private string PreBrainFile = "./Assets/PreBrains/NEBrain/NEBrain";
    void Awake() {
        // Debug.Log("AWAKE");
        if (nAgents != 2) {
            Debug.Log("Now, nAgents must be equal to 2.");
            nAgents = 2;
        }
        /*****
        Brainsをセットする
        *****/
        if (System.IO.File.Exists(GenerationFile)){
            StreamReader sr = new StreamReader(GenerationFile, Encoding.GetEncoding("Shift_JIS"));
            Generation = Int32.Parse(sr.ReadLine());
            Debug.Log("Start from Generation:");
            Debug.Log(Generation + 1);
            for(int i = 0; i < TotalPopulation; i++) {
                NNBrain SetBrain = new NNBrain(InputSize, HiddenSize, HiddenLayers, OutputSize);
                string BrainFile = PreBrainFile + i.ToString() + ".txt";
                SetBrain.Load(BrainFile);
                Brains.Add(SetBrain);
            }
        }
        else{
            for(int i = 0; i < TotalPopulation; i++) {
                NNBrain SetBrain = new NNBrain(InputSize, HiddenSize, HiddenLayers, OutputSize);
                Brains.Add(SetBrain);
            }
        }
        
        GObjects.Add(GObject1);
        Agents.Add(GObject1.GetComponent<HockeyAgent>());
        GObjects.Add(GObject2);
        Agents.Add(GObject2.GetComponent<HockeyAgent>());
        
        // tournament用のをここで選びたい
        SetStartAgents();
    }

    void SetStartAgents() {
        CurrentBrains = new Queue<NNBrain>(Brains);
        AgentsSet.Clear();
        var size = Math.Min(NAgents, TotalPopulation);
        for(var i = 0; i < size; i++) {
            AgentsSet.Add(new AgentPair {
                agent = Agents[i],
                brain = CurrentBrains.Dequeue()
            });
        }
    }

    public void Inactivate() {
        ManualModeFlag = true;
        GObject1.SetActive(false);
        GObject2.SetActive(false);
    }

    public void Activate() {
        ManualModeFlag = false;
        GObject1.SetActive(true);
        GObject2.SetActive(true);
    }

    public void Reset() {
        WaitingFlag = false;
    }

    // Agentが変わらない場合はRestart()が呼ばれる
    /***** Restart() Is Used When Agents Don't Change *****/
    public void Restart() {
        RestartFlag = false;
        AgentsSet.ForEach(p => { p.agent.TimeUp = false; });
    }

    void FixedUpdate() {
        if (WaitingFlag || RestartFlag || ManualModeFlag) {
            return;
        }
        /*****
        マイフレーム呼ばれて学習を進める関数
        ******/
        foreach(var pair in AgentsSet.Where(p => !p.agent.IsDone)) {
            // 観測・計算・実行
            AgentUpdate(pair.agent, pair.brain);
        }

        // tournamentのため勝者を取っておく
        NNBrain WinnerBrain = new NNBrain(InputSize, HiddenSize, HiddenLayers, OutputSize);
        float WinnerReward = 0.0f;

        AgentsSet.RemoveAll(p => {
            if(p.agent.IsDone) {
                float r = p.agent.Reward;
                BestRecord = Mathf.Max(r, BestRecord);
                GenBestRecord = Mathf.Max(r, GenBestRecord);
                p.brain.Reward = r;
                GenSumReward += r;
                //勝者記録 
                if (p.brain.Reward > WinnerReward){
                    WinnerReward = p.brain.Reward;
                    WinnerBrain = p.brain;
                }
            }
            if (p.agent.TimeUp) {
                RestartFlag = true;
            }
            return p.agent.IsDone;
        });
        
        if (WinnerReward > 0.0f){
            WinnerBrains.Add(WinnerBrain);
        }
        
        if(CurrentBrains.Count == 0 && AgentsSet.Count == 0 ) {
            Debug.Log(WinnerBrains.Count());
            SetNextGeneration();
        }
        else {
            SetNextAgents();
        }
    }

    private void AgentUpdate(Agent a, NNBrain b) {
        var observation = a.CollectObservations();
        var action = b.GetAction(observation);
        a.AgentAction(action); //only need fitness at the end
        //b.UpdateBrain(state, a.Reward) (QLearning)
    }

    private void SetNextAgents() {
        int size = Math.Min(NAgents - AgentsSet.Count, CurrentBrains.Count);
        for(var i = 0; i < size; i++) {
            var nextAgent = Agents.First(a => a.IsDone);
            var nextBrain = CurrentBrains.Dequeue();
            nextAgent.Reset();
            AgentsSet.Add(new AgentPair {
                agent = nextAgent,
                brain = nextBrain
            });
            // 今回の実装では使用しない
        }
        UpdateText();
    }

    private void SetNextGeneration() {
        GenAvgReward = GenSumReward / TotalPopulation;
        /*****
        PreBrainsを利用するためのセーブ
        *****/
        for (int i = 0; i< totalPopulation; i++){
            string BrainFile = PreBrainFile + i.ToString() + ".txt";
            Brains[i].Save(BrainFile);
        }
        using (StreamWriter sw = new StreamWriter(GenerationFile, false, Encoding.GetEncoding("Shift_JIS"))){
                sw.Write(Generation);
        }
        //new generation
        GenPopulation();
        GenSumReward = 0;
        GenBestRecord = 0;
        Agents.ForEach(a => a.Reset());
        SetStartAgents();
        UpdateText();
    }

    private static int CompareBrains(Brain a, Brain b) {
        if(a.Reward > b.Reward) return -1;
        if(b.Reward > a.Reward) return 1;
        return 0;
    }

    private void GenPopulation() {
        /*****
        新しい世代をNEで生成する
        *****/
        var children = new List<NNBrain>();
        var bestBrains = Brains.ToList();
        bestBrains.Sort(CompareBrains);
        // File.WriteAllText("BestBrain.json", JsonUtility.ToJson(bestBrains[0]));
        bestBrains[0].Save("./Assets/StreamingAssets/ComputerBrains/NEBest.txt");
        //Elite selection
        int ElitePop = Elite_size();
        for (int i = 0; i < ElitePop; i++) {
            children.Add(bestBrains[0]);
            bestBrains.RemoveAt(0);
        }
        // Debug.Log(bestBrains.Count);
        while(children.Count < TotalPopulation) {
            var tournamentMembers = bestBrains.AsEnumerable().OrderBy(x => Guid.NewGuid()).Take(tournamentSelection).ToList();
            tournamentMembers.Sort(CompareBrains);
            children.Add(tournamentMembers[0].Mutate(Generation).CrossOver(tournamentMembers[1],Generation));
            children.Add(tournamentMembers[1].Mutate(Generation).CrossOver(tournamentMembers[0],Generation));
            
        }
        Brains = children;
        Generation++;
    }

    public int Elite_size(){
        if (Generation < 20){
            return 0;
        }
        return 2;
    }

    private void UpdateText() {
        PopulationText.text = "Population: " + (TotalPopulation - CurrentBrains.Count) + "/" + TotalPopulation
            + "\nGeneration: " + (Generation + 1)
            + "\nBest Record: " + BestRecord
            + "\nBest this gen: " + GenBestRecord
            + "\nAverage: " + GenAvgReward;
    }

    private struct AgentPair
    {
        public NNBrain brain;
        public HockeyAgent agent;
    }
}
