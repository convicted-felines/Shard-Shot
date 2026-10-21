using GameFramework;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor.ResourceTools;
namespace MainGame.Editor
{
    public sealed class MainGameBuildEventHandler : IBuildEventHandler
    {
        public bool ContinueOnFailure
        {
            get
            {
                return false;
            }
        }

        public void OnPreprocessAllPlatforms(string productName, string companyName, string gameIdentifier, string gameFrameworkVersion, string unityVersion, string applicableGameVersion, int internalResourceVersion,
            Platform platforms, AssetBundleCompressionType assetBundleCompression, string compressionHelperTypeName, bool additionalCompressionSelected, bool forceRebuildAssetBundleSelected, string buildEventHandlerTypeName, string outputDirectory, BuildAssetBundleOptions buildAssetBundleOptions,
            string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, string buildReportPath)
        {
            string streamingAssetsPath = Utility.Path.GetRegularPath(Path.Combine(Application.dataPath, "StreamingAssets"));
            string[] fileNames = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);
            foreach (string fileName in fileNames)
            {
                if (fileName.Contains(".gitkeep"))
                {
                    continue;
                }

                File.Delete(fileName);
            }

            Utility.Path.RemoveEmptyDirectory(streamingAssetsPath);
        }

        public void OnPostprocessAllPlatforms(string productName, string companyName, string gameIdentifier, string gameFrameworkVersion, string unityVersion, string applicableGameVersion, int internalResourceVersion,
            Platform platforms, AssetBundleCompressionType assetBundleCompression, string compressionHelperTypeName, bool additionalCompressionSelected, bool forceRebuildAssetBundleSelected, string buildEventHandlerTypeName, string outputDirectory, BuildAssetBundleOptions buildAssetBundleOptions,
            string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, string buildReportPath)
        {
        }

        public void OnPreprocessPlatform(Platform platform, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath)
        {
        }

        public void OnBuildAssetBundlesComplete(Platform platform, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, AssetBundleManifest assetBundleManifest)
        {
        }

        public void OnOutputUpdatableVersionListData(Platform platform, string versionListPath, int versionListLength, int versionListHashCode, int versionListCompressedLength, int versionListCompressedHashCode)
        {
        }

        public void OnPostprocessPlatform(Platform platform, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, bool isSuccess)
        {
            if (!isSuccess)
            {
                return;
            }

            Platform activePlatform = GetActivePlatform();
            if (activePlatform == Platform.Undefined)
            {
                Debug.LogWarning(Utility.Text.Format("Skip copying resources to StreamingAssets: unsupported active build target '{0}'.", EditorUserBuildSettings.activeBuildTarget));
                return;
            }

            if (platform != activePlatform)
            {
                return;
            }

            string sourcePath = GetStreamingAssetsSourcePath(outputPackageSelected, outputPackagePath, outputPackedSelected, outputPackedPath);
            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogWarning("Skip copying resources to StreamingAssets: neither Output Package nor Output Packed is selected.");
                return;
            }

            if (!Directory.Exists(sourcePath))
            {
                Debug.LogWarning(Utility.Text.Format("Skip copying resources to StreamingAssets: source path '{0}' does not exist.", sourcePath));
                return;
            }

            string streamingAssetsPath = Utility.Path.GetRegularPath(Path.Combine(Application.dataPath, "StreamingAssets"));
            CopyDirectory(sourcePath, streamingAssetsPath);
            AssetDatabase.Refresh();
            Debug.Log(Utility.Text.Format("Copied '{0}' resources to StreamingAssets for platform '{1}'.", sourcePath, platform));
        }

        private static Platform GetActivePlatform()
        {
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows:
                    return Platform.Windows;

                case BuildTarget.StandaloneWindows64:
                    return Platform.Windows64;

#if UNITY_2017_3_OR_NEWER
                case BuildTarget.StandaloneOSX:
                    return Platform.MacOS;
#else
                case BuildTarget.StandaloneOSXUniversal:
                    return Platform.MacOS;
#endif
                case BuildTarget.StandaloneLinux64:
                    return Platform.Linux;

                case BuildTarget.iOS:
                    return Platform.IOS;

                case BuildTarget.Android:
                    return Platform.Android;

                case BuildTarget.WSAPlayer:
                    return Platform.WindowsStore;

                case BuildTarget.WebGL:
                    return Platform.WebGL;

                default:
                    return Platform.Undefined;
            }
        }

        private static string GetStreamingAssetsSourcePath(bool outputPackageSelected, string outputPackagePath, bool outputPackedSelected, string outputPackedPath)
        {
            if (outputPackageSelected)
            {
                return outputPackagePath;
            }

            if (outputPackedSelected)
            {
                return outputPackedPath;
            }

            return null;
        }

        private static void CopyDirectory(string sourcePath, string destinationPath)
        {
            sourcePath = Utility.Path.GetRegularPath(sourcePath);
            destinationPath = Utility.Path.GetRegularPath(destinationPath);

            string[] fileNames = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
            foreach (string fileName in fileNames)
            {
                string destFileName = Utility.Path.GetRegularPath(Path.Combine(destinationPath, fileName.Substring(sourcePath.Length)));
                FileInfo destFileInfo = new FileInfo(destFileName);
                if (!destFileInfo.Directory.Exists)
                {
                    destFileInfo.Directory.Create();
                }

                File.Copy(fileName, destFileName, true);
            }
        }
    }
}
