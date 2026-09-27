using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 기기에만 저장하는 진행 기록(persistentDataPath/save.json). 스테이지를 완성할 때만 쓰고,
    /// 판 도중의 상태는 저장하지 않는다(나가면 그 판은 처음부터 다시 한다).
    /// 쓰는 도중 앱이 꺼져도 파일이 깨지지 않게 임시 파일에 쓴 뒤 바꿔 끼운다.
    /// </summary>
    public static class SaveStore
    {
        const int Version = 1;
        const string FileName = "save.json";

        [Serializable]
        sealed class StageRecord
        {
            public string id;
            public int stars;

            /// <summary>처음 완성한 시각(UTC, Unix 초).</summary>
            public long clearedAt;
        }

        [Serializable]
        sealed class SaveFile
        {
            public int version = Version;
            public List<StageRecord> stages = new List<StageRecord>();
        }

        static Dictionary<string, StageRecord> records;

        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        static Dictionary<string, StageRecord> Records
        {
            get
            {
                if (records == null)
                {
                    Load();
                }

                return records;
            }
        }

        public static int Stars(string stageId)
        {
            return Records.TryGetValue(stageId, out StageRecord record) ? record.stars : 0;
        }

        /// <summary>더 좋은 별점일 때만 바꾸고 바로 파일에 쓴다.</summary>
        public static void RecordStars(string stageId, int stars)
        {
            if (Records.TryGetValue(stageId, out StageRecord record))
            {
                if (stars <= record.stars)
                {
                    return;
                }

                record.stars = stars;
            }
            else
            {
                Records[stageId] = new StageRecord
                {
                    id = stageId,
                    stars = stars,
                    clearedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                };
            }

            Write();
        }

        public static void Remove(string stageId)
        {
            if (Records.Remove(stageId))
            {
                Write();
            }
        }

        static void Load()
        {
            records = new Dictionary<string, StageRecord>();
            try
            {
                if (File.Exists(FilePath))
                {
                    var file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(FilePath));
                    foreach (StageRecord record in file?.stages ?? new List<StageRecord>())
                    {
                        if (!string.IsNullOrEmpty(record.id))
                        {
                            records[record.id] = record;
                        }
                    }

                    return;
                }
            }
            catch (Exception e)
            {
                // 파일이 깨졌으면 지우지 않고 옆에 남겨 둔 뒤 빈 기록으로 시작한다(되살릴 여지를 둔다).
                Debug.LogError("[Save] 저장 파일을 읽지 못했습니다: " + e.Message);
                TryBackupBroken();
                return;
            }

            MigrateFromPlayerPrefs();
        }

        /// <summary>저장 파일이 생기기 전 버전은 별점을 PlayerPrefs("stars.{id}")에 저장했다. 한 번 옮겨 온다.</summary>
        static void MigrateFromPlayerPrefs()
        {
            bool any = false;
            foreach (Core.Stage stage in StageCatalog.All)
            {
                string key = "stars." + stage.Id;
                int stars = PlayerPrefs.GetInt(key, 0);
                if (stars <= 0)
                {
                    continue;
                }

                records[stage.Id] = new StageRecord { id = stage.Id, stars = stars };
                PlayerPrefs.DeleteKey(key);
                any = true;
            }

            if (any)
            {
                Write();
                PlayerPrefs.Save();
            }
        }

        static void Write()
        {
            var file = new SaveFile();
            file.stages.AddRange(records.Values);
            string json = JsonUtility.ToJson(file, true);
            string temp = FilePath + ".tmp";
            try
            {
                File.WriteAllText(temp, json);
                if (File.Exists(FilePath))
                {
                    try
                    {
                        File.Replace(temp, FilePath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(temp, FilePath, true);
                        File.Delete(temp);
                    }
                }
                else
                {
                    File.Move(temp, FilePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] 저장하지 못했습니다: " + e.Message);
            }
        }

        static void TryBackupBroken()
        {
            try
            {
                File.Copy(FilePath, FilePath + ".broken", true);
            }
            catch (Exception)
            {
                // 백업도 못 하면 그냥 넘어간다. 게임은 빈 기록으로 계속된다.
            }
        }
    }
}
