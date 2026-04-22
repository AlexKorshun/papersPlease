using System;
using UnityEngine;

public enum ScannerEmotion { Calm, Anxiety, Fear }

[Serializable]
public struct VisitorProfile
{
    public string PersonId;
    public string FirstName;
    public string LastName;
    public string BirthDate;
    public string BirthPlace;
    public Sprite Photo;
    public bool ShouldBeAllowed;
    public ScannerEmotion EmotionalState;
}

public static class VisitorProfileGenerator
{
    private static readonly string[] First = { "Igor", "Nina", "Viktor", "Alina", "Sergey", "Mila" };
    private static readonly string[] Last = { "Volkov", "Petrova", "Sokolov", "Morozov", "Kuznetsova", "Smirnov" };
    private static readonly string[] BirthPlaces = { "Orvech Vonor", "Paradizna", "East Grestin", "Vedor", "Lendiforma" };

    public static VisitorProfile Generate(PersonArchetypeSO archetype, int visitorIndex)
    {
        int seed = (archetype != null ? archetype.Id.GetHashCode() : 12345) ^ (visitorIndex * 1103515245);
        int x = Mix(seed);

        string first = First[Mathf.Abs(x) % First.Length];
        string last = Last[Mathf.Abs(x / 7) % Last.Length];
        string birthPlace = BirthPlaces[Mathf.Abs(x / 11) % BirthPlaces.Length];
        string birthDate = $"19{(Mathf.Abs(x / 17) % 10) + 70}-0{(Mathf.Abs(x / 19) % 9) + 1}-1{Mathf.Abs(x / 23) % 9}";
        string personId = $"P-{Mathf.Abs(x) % 900000 + 100000}";

        return new VisitorProfile
        {
            PersonId = personId,
            FirstName = first,
            LastName = last,
            BirthDate = birthDate,
            BirthPlace = birthPlace,
            Photo = null,
            ShouldBeAllowed = true,
        };
    }

    public static PassportData BuildPassportData(in VisitorProfile profile, int visitorIndex, bool forged)
    {
        int x = Mix(profile.PersonId.GetHashCode() ^ (visitorIndex * 1664525));

        PassportData d = new PassportData
        {
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            BirthDate = profile.BirthDate,
            BirthPlace = profile.BirthPlace,
            Photo = profile.Photo,
        };

        if (forged)
        {
            // Minimal controlled mismatch against the shared profile.
            // Later we can expand to weighted forgery types per-day.
            int mode = Mathf.Abs(x) % 2;
            if (mode == 0)
                d.LastName = d.LastName + " Jr";
            else
                d.BirthPlace = BirthPlaces[(Array.IndexOf(BirthPlaces, d.BirthPlace) + 1) % BirthPlaces.Length];
        }

        return d;
    }

    public static PermitData BuildPermitData(in VisitorProfile profile, string todayDate, int visitorIndex, bool forged)
    {
        int x = Mix(profile.PersonId.GetHashCode() ^ (visitorIndex * 1013904223));

        string valid = todayDate;
        if (forged)
        {
            int delta = (Mathf.Abs(x) % 2 == 0) ? -1 : -2;
            if (System.DateTime.TryParseExact(todayDate, "dd.MM.yyyy", null,
                    System.Globalization.DateTimeStyles.None, out var dt))
            {
                valid = dt.AddDays(delta).ToString("dd.MM.yyyy");
            }
        }

        return new PermitData
        {
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            ValidDate = valid,
        };
    }

    // Legit:  65% Calm, 25% Anxiety, 10% Fear
    // Forged: 20% Calm, 50% Anxiety, 30% Fear
    public static ScannerEmotion GenerateEmotionalState(in VisitorProfile profile, bool hasForgedDoc, int visitorIndex)
    {
        int x = Mix(profile.PersonId.GetHashCode() ^ unchecked(visitorIndex * (int)2246822519u));
        float t = (Mathf.Abs(x) % 100) / 100f;

        if (!hasForgedDoc)
            return t < 0.65f ? ScannerEmotion.Calm : t < 0.90f ? ScannerEmotion.Anxiety : ScannerEmotion.Fear;
        else
            return t < 0.20f ? ScannerEmotion.Calm : t < 0.70f ? ScannerEmotion.Anxiety : ScannerEmotion.Fear;
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

