using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using System.IO;
public static class EnergyRushBuilder {
 public static void Build(){
  EditorUserBuildSettings.development=false; EditorUserBuildSettings.allowDebugging=false; EditorUserBuildSettings.connectProfiler=false;
  PlayerSettings.productName="Energy Rush"; PlayerSettings.companyName="Energy Rush";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=800;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
  var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedProperties();}
  Directory.CreateDirectory("Assets/Resources"); if(!File.Exists("Assets/Resources/StationShader.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),"Assets/Resources/StationShader.mat");
  if(!File.Exists("Assets/Materials/StationSurface.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")){color=new Color(.13f,.65f,.83f)},"Assets/Materials/StationSurface.mat");
  if(!File.Exists("Assets/Scenes/EnergyRush.unity")){var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Energy Rush Game").AddComponent<EnergyRush>();EditorSceneManager.SaveScene(scene,"Assets/Scenes/EnergyRush.unity");}
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/EnergyRush.unity",true)};AssetDatabase.SaveAssets();
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/EnergyRush.unity"},locationPathName="Builds/ReleaseCandidate/Energy Rush.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
  File.WriteAllText("Builds/ReleaseBuildResult.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings);if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Build failed");
 }
}
