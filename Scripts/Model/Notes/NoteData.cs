using System;

[Serializable]
public class NoteData
{
    public float beat;        // Время в битах от начала песни
    public int direction;     // 0-влево, 1-вверх, 2-вниз, 3-вправо

    public NoteData(float beat, int direction)
    {
        this.beat = beat;
        this.direction = direction;
    }
}