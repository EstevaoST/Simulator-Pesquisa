using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class SimGlobalConfig
{
    public const string GeneralKey = "Global/";
    public const string framesToUpdateKey = GeneralKey + "Global/";
    public static Prefs<int> framesToUpdate = new Prefs<int>(GeneralKey + "framesToUpdate", 55);

    public static readonly CultureInfo ExportFormat = CultureInfo.InvariantCulture;    
}

public class Prefs<E>
{    
    private string key;

    private E _value;
    public E value { 
        get { return _value; } 
        set { _value = value; Save(); }
    }

    public Prefs(string key, E value)
    {
        this.key = key;
        _value = value;
        Load();
    }

    private void Save()
    {
        if (_value == null)
            return;

        object aux = _value;
        if (value is string)
            PlayerPrefs.SetString(key, (string)aux);
        else if (value is int)
            PlayerPrefs.SetInt(key, (int)aux);
        else if (value is float)
            PlayerPrefs.SetFloat(key, (float)aux);
    }
    private void Load()
    {
        object aux = _value;
        if (value is string)
            aux = PlayerPrefs.GetString(key, (string)aux);
        else if(value is int)
            aux = PlayerPrefs.GetInt(key, (int)aux);
        else if (value is float)
            aux = PlayerPrefs.GetFloat(key, (float)aux);

        _value = (E)aux;
    }
}
