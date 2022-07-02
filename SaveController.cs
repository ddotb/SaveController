using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using UnityEngine;

public class SaveController : MonoBehaviour
{
    private SaveFile m_SaveFile = new SaveFile();

    private const string m_SaveFileName = "SaveGame.sav";
    private const int m_SaveFileVersion = 1;
    
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (FileExists())
        {
            LoadFromFile();

            if ((int)m_SaveFile.GetValue(SaveValueKeys.SAVE_FILE_VERSION) != m_SaveFileVersion)
            {
                Debug.LogWarning("Save file version mismatch.");
            }
        }
    }

    private void Start()
    {
        SetValue(SaveValueKeys.SAVE_FILE_VERSION, m_SaveFileVersion);

        SaveToFile();
    }

    public object GetValue(string key)
    {
        return m_SaveFile.GetValue(key);
    }

    public void SetValue(string key, object value)
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
        m_SaveFile.SetValue(SaveValueKeys.SAVE_TIMESTAMP, DateTime.UtcNow);
        
        m_SaveFile.SaveToFile(m_SaveFileName);
    }
}

public class SaveFile
{
    private Dictionary<string, object> m_Dictionary = new Dictionary<string, object>();
    private JavaScriptSerializer m_Serializer = new JavaScriptSerializer();

	private const int m_ObscureInt = 64;

    public object GetValue(string key)
    {
        if (!m_Dictionary.ContainsKey(key))
        {
            return null;
        }
        
        return m_Dictionary[key];
    }

    public void SetValue(string key, object value)
    {
        m_Dictionary[key] = value;
    }

    public void LoadFromFile(string fileName)
    {
        string saveText = File.ReadAllText(Application.persistentDataPath + "/" + fileName);

        saveText = FromEncryptedBase64(saveText);
        
        m_Dictionary = m_Serializer.Deserialize<Dictionary<string, object>>(saveText);
    }
    
    public void SaveToFile(string fileName)
    {
        string saveText = m_Serializer.Serialize(m_Dictionary);

        saveText = ToEncryptedBase64(saveText);
        
        File.WriteAllText(Application.persistentDataPath + "/" + fileName, saveText);
    }

    private string FromEncryptedBase64(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return data;
        }

        byte[] byteArray = Convert.FromBase64String(data);

        Obsure(ref byteArray);
        
        return UTF8Encoding.UTF8.GetString(byteArray);
    }

    private string ToEncryptedBase64(string data)
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
    public const string COMPLETED_GAME = "CompletedGame";
}
