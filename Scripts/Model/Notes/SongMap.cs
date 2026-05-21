using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSongMap", menuName = "Rhythm Game/Song Map")]
public class SongMap : ScriptableObject
{
    public string songName;
    public string artistName;
    public float bpm;                    // Темп песни
    public AudioClip songClip;           // Аудиофайл
    public List<NoteData> notes;         // Список нот
    public float offset = 0f;            // Ручная синхронизация 
    public float hitWindowPerfect = 0.05f;  // Окно для Perfect
    public float hitWindowGreat = 0.1f;     // Окно для Great
    public float hitWindowGood = 0.2f;      // Окно для Good
}