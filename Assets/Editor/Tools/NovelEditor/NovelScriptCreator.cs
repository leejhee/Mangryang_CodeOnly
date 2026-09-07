using UnityEditor;
using UnityEngine;
using System.IO;

public static class NovelScriptCreator
{
    [MenuItem("Assets/Create/Novel/New Script", false, 80)]
    public static void CreateNovelScriptFile()
    {
        string folderPath = GetSelectedFolderPath();
        string filePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPath, "NewScript.novel"));

        File.WriteAllText(filePath, ""); // 빈 파일 생성
        AssetDatabase.Refresh();

        // 선택 + 이름 변경 상태 만들기
        var asset = AssetDatabase.LoadAssetAtPath<Object>(filePath);
        ProjectWindowUtil.ShowCreatedAsset(asset);
    }

    private static string GetSelectedFolderPath()
    {
        var obj = Selection.activeObject;
        if (obj == null)
            return "Assets";

        string path = AssetDatabase.GetAssetPath(obj);
        if (File.Exists(path))
            path = Path.GetDirectoryName(path);

        return path.Replace("\\", "/");
    }
}
