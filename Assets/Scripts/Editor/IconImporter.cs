using UnityEditor;
using UnityEngine;

namespace ColorMinesweeper.EditorTools
{
    /// <summary>
    /// Assets/Resources/Icons 의 아이콘 PNG 를 UI 에 쓰기 좋게 가져온다.
    /// 스프라이트(높이 1 유닛, 가운데 피벗)로 만들고, 색을 흰색으로 바꿔 투명도만 남긴다.
    /// 그래야 SpriteRenderer.color 로 버튼 글자색 등 원하는 색을 입힐 수 있다(받은 아이콘은 검은색이다).
    /// </summary>
    sealed class IconImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Resources/Icons/";

        public override uint GetVersion()
        {
            return 1;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
        }

        void OnPostprocessTexture(Texture2D texture)
        {
            if (!assetPath.StartsWith(Folder))
            {
                return;
            }

            Color32[] pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, pixels[i].a);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            // 픽셀 단위 크기가 아니라 높이 1 유닛이 되도록(다른 아이콘 스프라이트와 같은 기준).
            var importer = (TextureImporter)assetImporter;
            if (assetPath.StartsWith(Folder) && Mathf.Abs(importer.spritePixelsPerUnit - texture.height) > 0.01f)
            {
                importer.spritePixelsPerUnit = texture.height;
                EditorApplication.delayCall += () => AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        /// <summary>이 규칙이 생기기 전에 기본 설정으로 가져온 아이콘이 있으면 다시 가져온다.</summary>
        public static void ReimportIfNeeded()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                    importer.textureType != TextureImporterType.Sprite)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}
