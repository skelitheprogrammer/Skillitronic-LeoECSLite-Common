using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Skillitronic.LeoECSLite.Common.Editor
{
    internal sealed partial class TemplateGenerator : ScriptableObject
    {
        private const string TITLE = "LeoECS Lite Entity Descriptor template generator";
        
        public static string CreateTemplate(string proto, string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "Invalid filename";

            string ns = EditorSettings.projectGenerationRootNamespace.Trim();
            if (string.IsNullOrEmpty(EditorSettings.projectGenerationRootNamespace)) ns = "Client";

            proto = proto.Replace("#NS#", ns);
            proto = proto.Replace("#SCRIPTNAME#", SanitizeClassName(Path.GetFileNameWithoutExtension(fileName)));
            try
            {
                File.WriteAllText(AssetDatabase.GenerateUniqueAssetPath(fileName), proto);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            AssetDatabase.Refresh();
            return null;
        }

        private static string SanitizeClassName(string className)
        {
            StringBuilder sb = new();
            bool needUp = true;
            foreach (char c in className)
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(needUp ? char.ToUpperInvariant(c) : c);
                    needUp = false;
                }
                else
                {
                    needUp = true;
                }

            return sb.ToString();
        }

        private static string CreateTemplateInternal(string proto, string fileName)
        {
            string res = CreateTemplate(proto, fileName);
            if (res != null) EditorUtility.DisplayDialog(TITLE, res, "Close");

            return res;
        }

        private static string GetTemplateContent(string proto)
        {
            TemplateGenerator pathHelper = CreateInstance<TemplateGenerator>();
            string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(pathHelper)));
            DestroyImmediate(pathHelper);
            try
            {
                return File.ReadAllText(Path.Combine(path ?? "", proto));
            }
            catch
            {
                return null;
            }
        }

        private static string GetAssetPath()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path) && AssetDatabase.Contains(Selection.activeObject))
            {
                if (!AssetDatabase.IsValidFolder(path)) path = Path.GetDirectoryName(path);
            }
            else
            {
                path = "Assets";
            }

            return path;
        }

        private static Texture2D GetIcon()
        {
            return EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
        }

        private static void CreateAndRenameAsset(string fileName, Texture2D icon, Action<string> onSuccess)
        {
            CustomEndNameAction action = CreateInstance<CustomEndNameAction>();
            action.Callback = onSuccess;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, action, fileName, icon, null);
        }

        private sealed class CustomEndNameAction : EndNameEditAction
        {
            [NonSerialized] public Action<string> Callback;

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                Callback?.Invoke(pathName);
            }
        }
    }
}