using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public DEEnvironment de;
    public NEEnvironment ne;
    public ManualPlayer mp;
    public ComputerPlayer ap;
    public PackManager pm;
    public CheckBoxManager cbm;
    private string Mode;

    // Start is called before the first frame update
    void Start()
    {
       Mode = cbm.GetMode();
    }

    // Update is called once per frame
    void Update()
    {
        //学習モードの時、得点が入るとプレイヤー・パックの一をリセット
        if (Mode == "Auto") {
            // if (de.WaitingFlag) {
            if (ne.WaitingFlag) {
                // de.Reset();
                ne.Reset();
                pm.Reset();
            }
            // if (de.RestartFlag) {
            if (ne.RestartFlag) {
            //    de.Restart();
               ne.Restart();
               pm.Reset();
            }
        }

        //対戦モードの時は得点が入るとパックのみリセット
        if (Mode == "Manual") {
            if (pm.Goal) {
                pm.Reset();
            }
        }

        //モード切り替え時
        string CurrentMode = cbm.GetMode();
        if (Mode != CurrentMode) {
            Mode = CurrentMode;
            //手動プレイヤー、敵エージェントを非活性化
            //学習エージェントを活性化
            if (Mode == "Auto") {
                mp.Inactivate();
                ap.Inactivate();
                // de.Activate();
                ne.Activate();
            }
            //学習エージェントを非活性化
            //手動プレイヤー、敵エージェントを活性化
            if (Mode == "Manual") {
                // de.Inactivate();
                ne.Inactivate();
                mp.Activate();
                ap.Activate();

            }
        }
    }
}
