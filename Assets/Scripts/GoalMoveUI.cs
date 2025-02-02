using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GoalMoveUI : MonoBehaviour
{
    [Header("Crate UI")]
    [SerializeField] private SpriteRenderer crateIconImage;    
    [SerializeField] private SpriteRenderer targetIconImage;
    
    [Header("UI References")]
    [SerializeField] private TMP_Text goalText;  
    [SerializeField] private TMP_Text movesText;

   
    [HideInInspector] public BoardManager boardManager;
    [HideInInspector] public BoardConfig boardConfig;
    
    private void Start()
    {
        if (boardConfig != null)
        {
            if (crateIconImage != null && boardConfig.crateIcon != null)
                crateIconImage.sprite = boardConfig.crateIcon;
            if (targetIconImage != null && boardConfig.targetIcon != null)
                targetIconImage.sprite = boardConfig.targetIcon;
        }
        
    }

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
            int remainingBlocks = Mathf.Max(0, boardConfig.targetBlockGoal - boardManager.GetTargetBlocksDestroyed());
            goalText.text = remainingBlocks.ToString();
        }
    }
}