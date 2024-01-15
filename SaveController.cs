using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using UnityEngine;
using UnityEngine.Rendering;

public class SaveController : Singleton<SaveController>
{
    [SerializeField] private bool m_ObscureSave = true;
    
    private SaveFile m_SaveFile;

    private const string m_SaveFileName = "SaveGame.sav";
    private const int m_SaveFileVersion = 1;

    public bool ObscureSave => m_ObscureSave;
    
    private void Awake()
    {
        m_SaveFile = new SaveFile(this);
        
        DontDestroyOnLoad(gameObject);

        if (FileExists())
        {
            LoadFromFile();

            int saveFileVersion = Convert.ToInt32(m_SaveFile.GetValue(SaveValueKeys.SAVE_FILE_VERSION));
            
            if (saveFileVersion != m_SaveFileVersion)
            {
                Debug.LogWarning("Save file version mismatch.");
            }
        }
    }

    private void Start()
    {
        SetValue(SaveValueKeys.SAVE_FILE_VERSION, m_SaveFileVersion.ToString());

        SaveToFile();
    }

    public string GetValue(string key)
    {
        return m_SaveFile.GetValue(key);
    }

    public void SetValue(string key, string value)
    {
        m_SaveFile.SetValue(key, value);
        
        SaveToFile();
    }

    private bool FileExists()
    {
        return File.Exists(Application.persistentDataPath + "/" + m_SaveFileName);
    }
    
    private void LoadFromFile()
    {
        m_SaveFile.LoadFromFile(m_SaveFileName);
    }

    private void SaveToFile()
    {
        m_SaveFile.SetValue(SaveValueKeys.SAVE_TIMESTAMP, DateTime.UtcNow.ToString());
        
        m_SaveFile.SaveToFile(m_SaveFileName);
    }
}

public class SaveFile
{
    private SaveController m_SaveController;
    
    private SerializedDictionary<string, string> m_Dictionary = new SerializedDictionary<string, string>();

    private const int m_ObscureInt = 64;

    public SaveFile(SaveController controller)
    {
        m_SaveController = controller;
    }
    
    public string GetValue(string key)
    {
        if (!m_Dictionary.ContainsKey(key))
        {
            return null;
        }
        
        return m_Dictionary[key];
    }

    public void SetValue(string key, string value)
    {
        m_Dictionary[key] = value;
    }

    public void LoadFromFile(string fileName)
    {
        string saveText = File.ReadAllText(Application.persistentDataPath + "/" + fileName);

        if (m_SaveController.ObscureSave)
        {
            saveText = FromObscuredBase64(saveText);
        }

        m_Dictionary = JsonUtility.FromJson<SerializedDictionary<string, string>>(saveText);
    }
    
    public void SaveToFile(string fileName)
    {
        string saveText = JsonUtility.ToJson(m_Dictionary);

        if (m_SaveController.ObscureSave)
        {
            saveText = ToObscuredBase64(saveText);
        }

        File.WriteAllText(Application.persistentDataPath + "/" + fileName, saveText);
    }

    private string FromObscuredBase64(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return data;
        }

        byte[] byteArray = Convert.FromBase64String(data);
        
        Obscure(ref byteArray);

        return UTF8Encoding.UTF8.GetString(byteArray);
    }

    private string ToObscuredBase64(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return data;
        }

        byte[] byteArray = UTF8Encoding.UTF8.GetBytes(data);
        
        Obscure(ref byteArray);

        return Convert.ToBase64String(byteArray);
    }

    private void Obscure(ref byte[] byteArray)
    {
        for (int i = 0; i < byteArray.Length; i++)
        {
            byteArray[i] ^= m_ObscureInt;
        }
    }
}

public class SaveValueKeys
{
    public const string SAVE_FILE_VERSION = "SaveFileVersion";
    public const string SAVE_TIMESTAMP = "SaveTimestamp";
    public const string LAST_ROOM_VISITED = "LastRoomVisited";
    public const string LAST_DOOR_VISITED = "LastDoorVisited";
    public const string COMPLETED_GAME = "CompletedGame";
}
