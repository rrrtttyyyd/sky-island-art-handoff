using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyIsland;

public static class PrototypeBuild {
    [MenuItem("Sky Island/Prepare prototype scene")]
    public static void Prepare(){
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Directory.CreateDirectory("Assets/SkyIsland/Materials");
        var shared=AssetDatabase.LoadAssetAtPath<Material>("Assets/SkyIsland/Materials/Prototype.mat");
        if(shared==null){shared=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(shared,"Assets/SkyIsland/Materials/Prototype.mat");}
        var prototype=new GameObject("Sky Island prototype").AddComponent<SkyIslandPrototype>();
        prototype.SharedPrototypeMaterial=shared;
        NatureSampleReview.Assign(prototype);
        prototype.InterfaceFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/SkyIsland/Fonts/NotoSansCJKsc-Regular.otf");
        Directory.CreateDirectory("Assets/SkyIsland/Scenes");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/SkyIsland/Scenes/Prototype.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/SkyIsland/Scenes/Prototype.unity",true)};
        PlayerSettings.companyName="SkyIslandPrototype";PlayerSettings.productName="Sky Island Prototype";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.skyisland.prototype");
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        AssetDatabase.SaveAssets();Debug.Log("SKY_SCENE_READY");
    }
    public static void Validate(){
        int count=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL "+name);count++;};
        for(int q=-8;q<=8;q++)for(int r=-8;r<=8;r++){var h=new Hex(q,r);check(Hex.FromWorld(h.World).Equals(h),"world roundtrip");}
        var island=new HexIsland{Wood=24,Stone=16};check(island.Cells.Count==7,"7 start cells");check(!island.Place(new Hex(9,9),out _),"no remote build");
        var expansion=new Hex(2,0);int wood=island.Wood;check(island.Place(expansion,out _),"adjacent build");check(island.Wood==wood-3,"one debit");check(!island.Place(expansion,out _),"duplicate rejected");check(island.Wood==wood-3,"no double debit");
        check(!island.Remove(new Hex(0,0),new Hex(1,0),out _),"core protected");check(!island.Remove(expansion,expansion,out _),"player supported");
        check(island.Place(new Hex(3,0),out _),"chain build");check(!island.Remove(expansion,new Hex(0,0),out _),"no disconnected island");
        check(island.BuildWorkshop(new Hex(1,0),out _),"workshop occupies cell");check(!island.Remove(new Hex(1,0),new Hex(0,0),out _),"workshop protected");
        var snapshot=island.Snapshot(Vector3.zero);check(HexIsland.TryRestore(snapshot,out var restored)&&restored.Cells.Count==island.Cells.Count&&restored.Wood==island.Wood,"save roundtrip");
        check(!HexIsland.TryRestore(new IslandSave{cells=new[]{new Hex(0,0),new Hex(8,8)}},out _),"invalid disconnected save rejected");
        Directory.CreateDirectory("../evidence/unity");File.WriteAllText("../evidence/unity/model-tests.txt",$"PASS {count} assertions; Unity {Application.unityVersion}; UTC {DateTime.UtcNow:o}\nOffline model only: not server authority or network validation.");
        Debug.Log($"SKY_MODEL_TESTS_PASS {count}");
    }
    public static void BuildWindows(){Prepare();Validate();ConstructionReview.Validate();MovementReview.Validate();SurvivalReview.Validate();PanelReview.Validate();NatureSampleReview.Validate();NatureResourceReview.Validate();AdventureReview.Validate();HarborReview.Validate();Directory.CreateDirectory("../Builds/Windows");var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"../Builds/Windows/SkyIsland.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);Debug.Log("SKY_WINDOWS_BUILD_OK "+report.summary.totalSize);}
    public static void BuildAndroid(){
        Prepare();Validate();ConstructionReview.Validate();MovementReview.Validate();SurvivalReview.Validate();PanelReview.Validate();NatureSampleReview.Validate();NatureResourceReview.Validate();AdventureReview.Validate();HarborReview.Validate();
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel33;
        PlayerSettings.Android.useCustomKeystore=false;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
        EditorUserBuildSettings.buildAppBundle=false;
        Directory.CreateDirectory("../Builds/Android");
        var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"../Builds/Android/SkyIsland-v0.1.apk",BuildTarget.Android,BuildOptions.None);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Android build failed: "+report.summary.result);
        Debug.Log("SKY_ANDROID_BUILD_OK "+report.summary.totalSize);
    }
    public static void BuildWeb(){
        Prepare();Validate();ConstructionReview.Validate();MovementReview.Validate();SurvivalReview.Validate();PanelReview.Validate();NatureSampleReview.Validate();NatureResourceReview.Validate();AdventureReview.Validate();HarborReview.Validate();
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback=true;
        PlayerSettings.WebGL.dataCaching=true;
        PlayerSettings.WebGL.memorySize=256;
        PlayerSettings.defaultWebScreenWidth=1280;PlayerSettings.defaultWebScreenHeight=720;
        Directory.CreateDirectory("../Builds/Web");
        var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"../Builds/Web",BuildTarget.WebGL,BuildOptions.None);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("WebGL build failed: "+report.summary.result);
        Debug.Log("SKY_WEB_BUILD_OK "+report.summary.totalSize);
    }
    public static void BuildWebProbe(){
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("WebGL route probe").AddComponent<WebGLRouteProbe>();
        Directory.CreateDirectory("Assets/SkyIsland/Scenes");
        const string scene="Assets/SkyIsland/Scenes/NetworkProbe.unity";
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),scene);
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback=true;
        Directory.CreateDirectory("../Builds/WebProbe");
        var report=BuildPipeline.BuildPlayer(new[]{scene},"../Builds/WebProbe",BuildTarget.WebGL,BuildOptions.None);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("WebGL probe build failed: "+report.summary.result);
        Debug.Log("SKY_WEB_PROBE_BUILD_OK "+report.summary.totalSize);
    }
}
