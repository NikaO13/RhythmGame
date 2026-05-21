using UnityEngine;
using UnityEngine.InputSystem;

public class ArrowInput : MonoBehaviour
{
    private int directionCode;
    private bool isInHitArea = false;
    private RhythmGameManager gameManager;

    public int DirectionCode => directionCode;

    public void SetDirection(int direction) => directionCode = direction;

    public void SetNoteData(NoteData note, RhythmGameManager manager) => gameManager = manager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HitArea"))
        {
            isInHitArea = true;
            Debug.Log($"[ArrowInput] ★ СТРЕЛА ВОШЛА В ЗОНУ! Направление: {directionCode} ★");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("HitArea"))
        {
            isInHitArea = false;
            Debug.Log("[ArrowInput] Стрела вышла из зоны (Промах)");
        }
    }

    private void Update()
    {
        var pressedKey = -1;
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) pressedKey = 0;
        else if (Keyboard.current.upArrowKey.wasPressedThisFrame) pressedKey = 1;
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame) pressedKey = 2;
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame) pressedKey = 3;

        if (pressedKey != -1)
        {
            Debug.Log($"[ArrowInput] Нажата кнопка: {pressedKey}. В зоне? {isInHitArea}.");

            if (isInHitArea && gameManager != null)
            {
                gameManager.TryHitNote(pressedKey, this);
            }
        }
    }
}