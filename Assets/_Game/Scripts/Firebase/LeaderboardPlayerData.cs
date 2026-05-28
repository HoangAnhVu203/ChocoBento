using System;
using Firebase.Firestore;

[FirestoreData]
[Serializable]
public class LeaderboardPlayerData
{
    [FirestoreDocumentId]
    public string Uid { get; set; }

    [FirestoreProperty("displayName")]
    public string DisplayName { get; set; }

    [FirestoreProperty("highestCompletedLevel")]
    public int HighestCompletedLevel { get; set; }
}
