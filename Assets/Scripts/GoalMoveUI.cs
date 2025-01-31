using UnityEngine;
using TMPro;

public class GoalMoveUI : MonoBehaviour
{
    public TMP_Text goalText;
    public TMP_Text moveText;
    public BoardManager boardManager;

    private void Update()
    {
        if (!boardManager) return;
        moveText.text = boardManager.GetMovesLeft().ToString();
        goalText.text = (boardManager.boardConfig.goalBlocks - boardManager.GetBlocksDestroyed()).ToString();
    }
}