using System;
using System.IO;
using static System.Text.Encoding;
using UnityEngine;
using Zenject;

/// <summary>
/// セーブデータ保存クラス
/// </summary>
namespace SaveDataManager
{
    [System.Serializable]
    public class SaveDataManager : MonoBehaviour, ISaveDataManager
    {
        [SerializeField]
        [Header("セーブ完了後に表示するメッセージを設定します")]
        string _saveCompletedMessage = "データを保存しました";
        [SerializeField]
        [Header("セーブができなかった場合に表示するメッセージを設定します")]
        string _saveNotCompletedMessage = "データが保存できませんでした、再度お試しください";

        ISaveData _saveData;
        string _savePath;
        string _wegGlSaveKey = "SaveData";
        bool _isInitialized;

        public ISaveData SaveData => _saveData;
        public bool IsInitialized => _isInitialized;
        public string SaveCompletedMessage => _saveCompletedMessage;
        public string SaveNotCompletedMessage => _saveNotCompletedMessage;

        void Awake()
        {
            _savePath = Application.dataPath + "/playerData.json";
        }

        [Inject]
        public void Construct(ISaveData saveData)
        {
            _saveData = saveData;
        }

        public void SetStageNum(int stageNum)
        {
            _saveData.SetStageNum(stageNum);
        }

        public void SetScore(int score)
        {
            _saveData.SetScore(score);
        }

        /// <summary>
        /// 初期化フラグを設定する
        /// </summary>
        public void SetIsInitialized(bool isInitialized)
        {
            _isInitialized = isInitialized;
        }

        /// <summary>
        /// セーブデータが存在しているか
        /// </summary>
        /// <returns></returns>
        public bool SaveDataExists()
        {
#if UNITY_EDITOR
            return File.Exists(_savePath);

#elif UNITY_WEBGL
            return (String.IsNullOrEmpty(PlayerPrefs.GetString(_wegGlSaveKey)) == false);
#endif
        }

        /// <summary>
        /// データを保存します
        /// </summary>
        public bool Save()
        {
            string jsonStr = JsonUtility.ToJson(_saveData);

#if UNITY_EDITOR
            //ファイルに出力
            using (StreamWriter sw = new StreamWriter(_savePath, false, UTF8))
            {
                try
                {
                    sw.Write(jsonStr);
                    sw.Flush();
                    sw.Close();//念の為明記
                    return true;
                }
                catch
                {
                    Debug.Log("データを保存できませんでした。");
                    return false;
                }
            }

#elif UNITY_WEBGL
            //webGLはPlayerPrefsで保存する
            PlayerPrefs.SetString(_wegGlSaveKey, jsonStr);
            return true;
#endif
        }

        /// <summary>
        /// データを読み込みます
        /// </summary>
        public bool Load()
        {
#if UNITY_EDITOR
            using (StreamReader sr = new StreamReader(_savePath))
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(sr.ReadToEnd(), _saveData);
                    sr.Close();
                    return true;
                }
                catch
                {
                    Debug.Log("データを読み込めませんでした。");
                    return false;
                }
            }

#elif UNITY_WEBGL
            string jsonStr = PlayerPrefs.GetString(_wegGlSaveKey);
            JsonUtility.FromJsonOverwrite(jsonStr, _saveData);
            return true;
#endif
        }
    }
}
