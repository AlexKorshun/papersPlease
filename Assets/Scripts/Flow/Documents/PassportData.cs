using System;
using UnityEngine;

[Serializable]
public struct PassportData
{
    public string FullName;
    public string PassportNumber;
    public string Nationality;
    public string ExpiryDate; // keep as string for now (easy to format & localize)
    public Sprite Photo;
}

