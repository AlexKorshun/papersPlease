using System;
using UnityEngine;

[Serializable]
public struct VisitorProfile
{
    public string PersonId;
    public string FullName;
    public string Nationality;
    public Sprite Photo;
    public bool ShouldBeAllowed;
}

public static class VisitorProfileGenerator
{
    private static readonly string[] First = { "Igor", "Nina", "Viktor", "Alina", "Sergey", "Mila" };
    private static readonly string[] Last = { "Volkov", "Petrova", "Sokolov", "Morozov", "Kuznetsova", "Smirnov" };
    private static readonly string[] Nat = { "Arstotzka", "Kolechia", "Impor", "Antegria" };

    public static VisitorProfile Generate(PersonArchetypeSO archetype, int visitorIndex)
    {
        int seed = (archetype != null ? archetype.Id.GetHashCode() : 12345) ^ (visitorIndex * 1103515245);
        int x = Mix(seed);

        string first = First[Mathf.Abs(x) % First.Length];
        string last = Last[Mathf.Abs(x / 7) % Last.Length];
        string nationality = Nat[Mathf.Abs(x / 29) % Nat.Length];
        string personId = $"P-{Mathf.Abs(x) % 900000 + 100000}";

        return new VisitorProfile
        {
            PersonId = personId,
            FullName = $"{first} {last}",
            Nationality = nationality,
            Photo = null,
        };
    }

    public static PassportData BuildPassportData(in VisitorProfile profile, int visitorIndex, bool forged)
    {
        int x = Mix(profile.PersonId.GetHashCode() ^ (visitorIndex * 1664525));

        string passportNumber = $"AG-{Mathf.Abs(x / 13) % 900000 + 100000}";
        string expiry = $"19{(Mathf.Abs(x / 31) % 10) + 80}-0{(Mathf.Abs(x / 37) % 9) + 1}-1{Mathf.Abs(x / 41) % 9}";

        PassportData d = new PassportData
        {
            FullName = profile.FullName,
            Nationality = profile.Nationality,
            PassportNumber = passportNumber,
            ExpiryDate = expiry,
            Photo = profile.Photo,
        };

        if (forged)
        {
            // Minimal controlled mismatch against the shared profile.
            // Later we can expand to weighted forgery types per-day.
            int mode = Mathf.Abs(x) % 2;
            if (mode == 0)
                d.FullName = d.FullName + " Jr";
            else
                d.Nationality = Nat[(Array.IndexOf(Nat, d.Nationality) + 1) % Nat.Length];
        }

        return d;
    }

    private static int Mix(int x)
    {
        unchecked
        {
            x ^= (x << 13);
            x ^= (x >> 17);
            x ^= (x << 5);
            return x;
        }
    }
}

