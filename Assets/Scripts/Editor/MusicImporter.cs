using UnityEditor;
using UnityEngine;

namespace ColorMinesweeper.EditorTools
{
    /// <summary>
    /// Assets/Resources/Music 의 곡은 길고 계속 흐르므로 메모리에 다 풀지 않고 스트리밍으로 가져온다.
    /// 효과음과 달리 한 번에 하나만 재생되니 끊김 걱정 없이 용량을 아낀다.
    /// </summary>
    public sealed class MusicImporter : AssetPostprocessor
    {
        const string Folder = "Assets/Resources/Music/";

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Folder))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            importer.loadInBackground = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
        }
    }
}
