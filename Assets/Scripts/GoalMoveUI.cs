using UnityEngine;
using TMPro;

public class GoalMoveUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text goalText;  
    [SerializeField] private TMP_Text movesText;

   
    [HideInInspector] public BoardManager boardManager;
    [HideInInspector] public BoardConfig boardConfig;

    private void Update()
    {
        if (boardManager == null || boardConfig == null) return;
        movesText.text = boardManager.GetMovesLeft().ToString();
        if (boardConfig.useCrates)
        {
            int remainingCrates = Mathf.Max(0, boardConfig.targetCrateGoal - boardManager.GetTargetCratesDestroyed());
            goalText.text = remainingCrates.ToString();
        }
        else
        {
            int remainingBlocks = Mathf.Max(0, boardConfig.targetBlockGoal - boardManager.GetBlocksDestroyed());
            goalText.text = remainingBlocks.ToString();
        }
    }
}