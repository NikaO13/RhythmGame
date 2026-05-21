using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RhythmGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SongMap currentSong;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform hitAreaTransform;

    [Header("Effects Settings")]
    [SerializeField] private GameObject sparksPrefab;
    [SerializeField] private GameObject ratingTextPrefab;

    [Header("Rhythm Settings")]
    [SerializeField] private float spawnOffsetBeats = 2f;

    [Header("In-Game UI (Disappears at end)")]
    [SerializeField] private GameObject inGameUIPanel;
    [SerializeField] private UnityEngine.UI.Text scoreText;
    [SerializeField] private UnityEngine.UI.Text comboText;
    [SerializeField] private UnityEngine.UI.Text accuracyText;

    [Header("End Game Summary UI (Appears at end)")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private UnityEngine.UI.Text finalScoreText;
    [SerializeField] private UnityEngine.UI.Text finalMaxComboText;
    [SerializeField] private UnityEngine.UI.Text finalAccuracyText;

    private AudioSource musicSource;
    private double songStartTime;
    private int currentNoteIndex = 0;
    private int currentScore = 0;
    private int currentCombo = 0;
    private int maxCombo = 0;
    private int totalNotesProcessed = 0;
    private int perfectHits = 0;
    private int greatHits = 0;
    private int goodHits = 0;
    private int missHits = 0;
    private bool isPlaying = false;

    private Vector3 calculatedSpawnPos;
    private float calculatedArrowSpeed;
    private float hitAreaX;
    private float beatInterval;

    private Queue<GameObject> activeArrows = new Queue<GameObject>();
    private Dictionary<GameObject, NoteData> arrowToNote = new Dictionary<GameObject, NoteData>();
    private List<HitResult> hitHistory = new List<HitResult>();

    void Start()
    {
        musicSource = GetComponent<AudioSource>();
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();

        if (currentSong == null || hitAreaTransform == null)
        {
            Debug.LogError("[MANAGER] Не все ссылки назначены в Инспекторе!");
            return;
        }

        if (inGameUIPanel != null) inGameUIPanel.SetActive(true);
        if (endGamePanel != null) endGamePanel.SetActive(false);

        SetupScreenAdaptivePositions();
        StartCoroutine(DelayedStartRoutine());
    }

    private void SetupScreenAdaptivePositions()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var screenLeftX = cam.ScreenToWorldPoint(new Vector3(0, Screen.height / 2f, cam.nearClipPlane)).x;
        var screenRightX = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2f, cam.nearClipPlane)).x;
        var padding = (screenRightX - screenLeftX) * 0.1f;

        hitAreaX = screenLeftX + padding;
        hitAreaTransform.position = new Vector3(hitAreaX, hitAreaTransform.position.y, hitAreaTransform.position.z);

        var spawnX = screenRightX - padding;
        calculatedSpawnPos = new Vector3(spawnX, hitAreaTransform.position.y, 0f);

        beatInterval = 60f / currentSong.bpm;
        var travelTime = spawnOffsetBeats * beatInterval;
        var distance = Mathf.Abs(spawnX - hitAreaX);

        calculatedArrowSpeed = distance / travelTime;
    }

    private IEnumerator DelayedStartRoutine()
    {
        yield return null;
        StartGame();
    }

    void Update()
    {
        if (!isPlaying || currentSong == null) return;

        var currentTime = AudioSettings.dspTime - songStartTime;
        var currentBeatTime = (float)(currentTime * currentSong.bpm / 60f);

        while (currentNoteIndex < currentSong.notes.Count &&
               currentSong.notes[currentNoteIndex].beat - spawnOffsetBeats <= currentBeatTime)
        {
            SpawnNote(currentSong.notes[currentNoteIndex]);
            currentNoteIndex++;
        }

        while (activeArrows.Count > 0 && activeArrows.Peek().transform.position.x < (hitAreaX - 1.3f))
        {
            var missedArrow = activeArrows.Dequeue();
            arrowToNote.Remove(missedArrow);
            OnNoteMiss();
            Destroy(missedArrow);
        }

        CheckSongEnd();
    }

    private void SpawnNote(NoteData note)
    {
        var rotation = note.direction switch
        {
            0 => Quaternion.Euler(0, 0, 180),
            1 => Quaternion.Euler(0, 0, 90),
            2 => Quaternion.Euler(0, 0, -90),
            3 => Quaternion.Euler(0, 0, 0),
            _ => Quaternion.identity
        };

        var arrow = Instantiate(arrowPrefab, calculatedSpawnPos, rotation);
        var arrowInput = arrow.GetComponent<ArrowInput>();

        if (arrowInput != null)
        {
            arrowInput.SetDirection(note.direction);
            arrowInput.SetNoteData(note, this);
        }

        var arrowRigidbody = arrow.GetComponent<Rigidbody2D>();
        if (arrowRigidbody != null) arrowRigidbody.linearVelocity = Vector2.left * calculatedArrowSpeed;

        activeArrows.Enqueue(arrow);
        arrowToNote[arrow] = note;
    }

    public void TryHitNote(int pressedDirection, ArrowInput arrowScript)
    {
        if (!isPlaying || activeArrows.Count == 0) return;

        var firstArrow = activeArrows.Peek();
        if (firstArrow != null && firstArrow != arrowScript.gameObject) return;

        var note = arrowToNote[firstArrow];

        if (pressedDirection == note.direction)
        {
            var distanceToTarget = Mathf.Abs(firstArrow.transform.position.x - hitAreaX);
            var hitResult = distanceToTarget <= 0.35f ? HitResult.Perfect :
                            distanceToTarget <= 0.75f ? HitResult.Great :
                            distanceToTarget <= 1.25f ? HitResult.Good : HitResult.Miss;

            if (hitResult == HitResult.Miss)
            {
                OnNoteMiss();
            }
            else
            {
                Debug.Log($"<color=green>[HIT] {hitResult} | Дистанция: {distanceToTarget:F3}</color>");
                AddScore(hitResult);
                InstantiateSparksEffect();
                SpawnRatingText(hitResult);
            }

            var hitAreaScript = hitAreaTransform.GetComponent<HitArea>();
            if (hitAreaScript != null) hitAreaScript.Flash();
        }
        else
        {
            Debug.LogWarning($"[HIT] Нажато неверное направление! Сброс комбо.");
            OnNoteMiss();
        }

        activeArrows.Dequeue();
        arrowToNote.Remove(firstArrow);
        Destroy(firstArrow);
    }

    private void InstantiateSparksEffect()
    {
        if (sparksPrefab != null && hitAreaTransform != null)
        {
            var spawnPosition = new Vector3(hitAreaTransform.position.x, hitAreaTransform.position.y, hitAreaTransform.position.z - 0.1f);
            var sparks = Instantiate(sparksPrefab, spawnPosition, Quaternion.identity);
            Destroy(sparks, 0.5f);
        }
    }

    private void SpawnRatingText(HitResult result)
    {
        if (ratingTextPrefab == null || inGameUIPanel == null || hitAreaTransform == null) return;

        var textObject = Instantiate(ratingTextPrefab, inGameUIPanel.transform);
        var rectText = textObject.GetComponent<RectTransform>();

        if (rectText != null)
        {
            var hitAreaWorldPos = hitAreaTransform.position;
            rectText.position = new Vector3(hitAreaWorldPos.x, hitAreaWorldPos.y + 1.5f, rectText.position.z);
        }

        var ratingScript = textObject.GetComponent<HitRatingText>() ?? textObject.AddComponent<HitRatingText>();

        var (textText, textColor) = result switch
        {
            HitResult.Perfect => ("PERFECT", new Color(1f, 0f, 0.66f)),
            HitResult.Great => ("GREAT", new Color(1f, 0.5f, 0f)),
            HitResult.Good => ("GOOD", new Color(0f, 1f, 0f)),
            _ => ("MISS", new Color(1f, 0f, 0f))
        };

        ratingScript.Setup(textText, textColor);
    }

    private void AddScore(HitResult result)
    {
        totalNotesProcessed++;
        hitHistory.Add(result);

        var comboMultiplier = Mathf.Min(1 + (currentCombo / 10), 5);
        var baseScore = 0;

        switch (result)
        {
            case HitResult.Perfect:
                baseScore = 100;
                perfectHits++;
                break;
            case HitResult.Great:
                baseScore = 70;
                greatHits++;
                break;
            case HitResult.Good:
                baseScore = 50;
                goodHits++;
                break;
        }

        currentCombo++;
        currentScore += baseScore * comboMultiplier;
        if (currentCombo > maxCombo) maxCombo = currentCombo;

        UpdateUI();
    }

    private void OnNoteMiss()
    {
        currentCombo = 0;
        missHits++;
        totalNotesProcessed++;
        hitHistory.Add(HitResult.Miss);

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"Score: {currentScore}";
        if (comboText != null) comboText.text = $"Combo: x{currentCombo}";

        if (accuracyText != null)
        {
            accuracyText.text = totalNotesProcessed > 0
                ? $"Accuracy: {((perfectHits * 1f + greatHits * 0.7f + goodHits * 0.4f) / totalNotesProcessed * 100f):F1}%"
                : "Accuracy: 100.0%";
        }
    }

    private void CheckSongEnd()
    {
        if (isPlaying && !musicSource.isPlaying && currentNoteIndex > 0)
        {
            isPlaying = false;
            Debug.Log("[MANAGER] Аудио завершено. Показываем сводку результатов.");

            if (inGameUIPanel != null) inGameUIPanel.SetActive(false);
            if (hitAreaTransform != null) hitAreaTransform.gameObject.SetActive(false);

            var totalSuccessfulHits = hitHistory.Where(h => h != HitResult.Miss).Count();
            var favoriteRating = hitHistory.Count > 0
                ? hitHistory.GroupBy(h => h).OrderByDescending(g => g.Count()).Select(g => g.Key.ToString()).FirstOrDefault()
                : "None";

            Debug.Log($"[LINQ STATS] Успешных попаданий: {totalSuccessfulHits} из {hitHistory.Count}. Чаще всего получали оценку: {favoriteRating}");

            var finalAccuracy = totalNotesProcessed > 0
                ? (perfectHits * 1f + greatHits * 0.7f + goodHits * 0.4f) / totalNotesProcessed * 100f
                : 100f;

            if (finalScoreText != null) finalScoreText.text = $"Итоговый Счёт: {currentScore}";
            if (finalMaxComboText != null) finalMaxComboText.text = $"Макс. Комбо: x{maxCombo}";
            if (finalAccuracyText != null) finalAccuracyText.text = $"Точность: {finalAccuracy:F1}%";

            if (endGamePanel != null) endGamePanel.SetActive(true);
        }
    }

    public void ChangeSceneToMenu(int sceneIndex)
    {
        StopGame();
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
    }

    public void StartGame()
    {
        isPlaying = true;
        currentScore = 0;
        currentCombo = 0;
        maxCombo = 0;
        totalNotesProcessed = 0;
        perfectHits = 0;
        greatHits = 0;
        goodHits = 0;
        missHits = 0;
        hitHistory.Clear();

        if (hitAreaTransform != null) hitAreaTransform.gameObject.SetActive(true);

        musicSource.clip = currentSong.songClip;
        var travelTime = spawnOffsetBeats * beatInterval;
        songStartTime = AudioSettings.dspTime;
        musicSource.PlayScheduled(AudioSettings.dspTime + travelTime);

        UpdateUI();
    }

    public void StopGame()
    {
        isPlaying = false;
        musicSource.Stop();
        activeArrows.Where(arrow => arrow != null).ToList().ForEach(Destroy);

        activeArrows.Clear();
        arrowToNote.Clear();
    }

    private void OnDestroy() => StopGame();
    public enum HitResult { Perfect, Great, Good, Miss }
}