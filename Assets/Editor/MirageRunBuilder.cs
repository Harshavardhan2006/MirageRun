using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class MirageRunBuilder
{
    const float TW = 9f;
    const float L = 60f;
    const string HeroRigged = "Assets/Models/Character/Hero.fbx";
    const string HeroCombined = "Assets/Models/Character/Hero_Running.fbx";
    const string HeroStatic = "Assets/Models/Character/base_basic_pbr.fbx";
    const string RunFbx = "Assets/Animations/Running.fbx";

    static float heroYaw;
    static bool riggedHero;
    static bool useOwnMaterials;
    static GameObject heroSource;
    static AnimationClip runClip;
    static Avatar heroAvatar;
    static RuntimeAnimatorController heroController;

    static Material mHero, mTrack, mSand, mLine, mCrate, mCrateDark, mRock, mCactus, mTrunk, mLeaf, mMesa1, mMesa2, mMesa3, mGold, mGem, mGate, mParticle, mDark;
    static PhysicsMaterial noFriction;
    static Mesh gemMesh;
    static Sprite uiSprite, knobSprite;
    static Material skyMat;
    static VolumeProfile fxProfile;
    static Mesh duneLeft, duneRight;
    static Mesh[] rockMeshes;

    [MenuItem("Mirage Run/Build Everything")]
    static void Build() { heroYaw = 0f; Run(); }

    [MenuItem("Mirage Run/Build Everything (turn hero 180 degrees)")]
    static void Build180() { heroYaw = 180f; Run(); }

    static void Run()
    {
        if (!EnsureInput()) return;
        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog("TextMeshPro needed", "Use Window > TextMeshPro > Import TMP Essential Resources, click Import, then run Mirage Run > Build Everything again.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            EditorUtility.DisplayProgressBar("Mirage Run", "Preparing folders", 0.05f);
            EnsureFolders();
            PlayerSettings.productName = "Mirage Run";
            uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            EditorUtility.DisplayProgressBar("Mirage Run", "Importing character", 0.15f);
            ConfigureTextures();
            MakeMaterials();
            LoadEnvironment();
            MakeAtmosphere();
            PrepareHero();

            EditorUtility.DisplayProgressBar("Mirage Run", "Building prefabs", 0.4f);
            GameObject playerPrefab = BuildPlayerPrefab();
            GameObject startSegment;
            GameObject[] segments = BuildSegments(out startSegment);

            EditorUtility.DisplayProgressBar("Mirage Run", "Building scenes", 0.7f);
            string menuPath = BuildMainMenu(startSegment);
            string stagePath = BuildStageSelect();
            string infoPath = BuildInformation();
            string gamePath = BuildGamePlay(playerPrefab, startSegment, segments);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(menuPath, true),
                new EditorBuildSettingsScene(stagePath, true),
                new EditorBuildSettingsScene(infoPath, true),
                new EditorBuildSettingsScene(gamePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(menuPath);
            EditorUtility.ClearProgressBar();
            string mode = riggedHero ? "Rigged hero with the running animation was used." : "Static hero was used with a procedural run bob. Add Hero.fbx from Mixamo and build again to get the real animation.";
            EditorUtility.DisplayDialog("Mirage Run is ready", mode + "\n\nMainMenu is open. Press Play.", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("Mirage Run build failed: " + e);
        }
    }

    static bool EnsureInput()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0) return true;
        SerializedObject so = new SerializedObject(assets[0]);
        SerializedProperty p = so.FindProperty("activeInputHandler");
        if (p == null || p.intValue != 1) return true;
        p.intValue = 2;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Restart Unity", "The project used only the new Input System. It is now set to Both so the game controls work. Restart the Unity Editor when asked (or close and reopen the project), then run Mirage Run > Build Everything again.", "OK");
        return false;
    }

    static void EnsureFolders()
    {
        foreach (string d in new[] { "Assets/Scenes", "Assets/Prefabs", "Assets/Materials", "Assets/Animations", "Assets/Meshes" })
            Directory.CreateDirectory(d);
        AssetDatabase.Refresh();
    }

    static T Replace<T>(T obj, string path) where T : Object
    {
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(obj, path);
        return obj;
    }

    static void ConfigureTextures()
    {
        foreach (string file in Directory.GetFiles("Assets/Textures/Desert", "*.png"))
        {
            string path = file.Replace("\\", "/");
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            if (path.EndsWith("_normal.png")) ti.textureType = TextureImporterType.NormalMap;
            ti.maxTextureSize = 1024;
            ti.anisoLevel = 8;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.SaveAndReimport();
        }
        TextureImporter n = AssetImporter.GetAtPath("Assets/Textures/Character/texture_normal.png") as TextureImporter;
        if (n != null)
        {
            n.textureType = TextureImporterType.NormalMap;
            n.maxTextureSize = 2048;
            n.SaveAndReimport();
        }
        TextureImporter d = AssetImporter.GetAtPath("Assets/Textures/Character/texture_diffuse.png") as TextureImporter;
        if (d != null)
        {
            d.maxTextureSize = 2048;
            d.SaveAndReimport();
        }
    }

    static Shader LitShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        return s != null ? s : Shader.Find("Standard");
    }

    static Material Mat(string name, Color color, float smooth = 0.05f, Color? emission = null)
    {
        Material m = new Material(LitShader());
        m.name = name;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        return Replace(m, "Assets/Materials/" + name + ".mat");
    }

    static void MakeMaterials()
    {
        mTrack = Mat("Track", new Color(0.93f, 0.78f, 0.46f));
        mSand = Mat("Sand", new Color(0.86f, 0.66f, 0.34f));
        mLine = Mat("TrackLine", new Color(0.82f, 0.64f, 0.34f));
        mCrate = Mat("Crate", new Color(0.62f, 0.4f, 0.2f));
        mCrateDark = Mat("CrateDark", new Color(0.38f, 0.24f, 0.12f));
        mRock = Mat("Rock", new Color(0.56f, 0.56f, 0.6f), 0.15f);
        mCactus = Mat("Cactus", new Color(0.24f, 0.56f, 0.27f));
        mTrunk = Mat("Trunk", new Color(0.46f, 0.29f, 0.14f));
        mLeaf = Mat("Leaf", new Color(0.2f, 0.6f, 0.25f));
        mMesa1 = Mat("Mesa1", new Color(0.78f, 0.4f, 0.22f));
        mMesa2 = Mat("Mesa2", new Color(0.68f, 0.33f, 0.2f));
        mMesa3 = Mat("Mesa3", new Color(0.86f, 0.52f, 0.29f));
        mGold = Mat("Gold", new Color(1f, 0.78f, 0.15f), 0.6f, new Color(1.8f, 1.15f, 0.1f));
        mGem = Mat("Gem", new Color(0.9f, 0.08f, 0.16f), 0.7f, new Color(1.6f, 0.05f, 0.2f));
        mGate = Mat("Gate", new Color(0.4f, 0.25f, 0.15f));
        mDark = Mat("DarkBackdrop", new Color(0.12f, 0.08f, 0.06f));

        Shader ps = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (ps == null) ps = Shader.Find("Sprites/Default");
        Material pm = new Material(ps);
        pm.name = "PickupParticle";
        if (pm.HasProperty("_BaseColor")) pm.SetColor("_BaseColor", Color.white);
        Texture2D soft = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Desert/soft_dust.png");
        if (soft != null && pm.HasProperty("_BaseMap")) pm.SetTexture("_BaseMap", soft);
        if (pm.HasProperty("_Surface"))
        {
            pm.SetFloat("_Surface", 1f);
            pm.SetFloat("_Blend", 0f);
            pm.SetOverrideTag("RenderType", "Transparent");
            pm.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            pm.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            pm.SetInt("_ZWrite", 0);
            pm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            pm.renderQueue = (int)RenderQueue.Transparent;
        }
        mParticle = Replace(pm, "Assets/Materials/PickupParticle.mat");

        PhysicsMaterial f = new PhysicsMaterial("NoFriction");
        f.dynamicFriction = 0f;
        f.staticFriction = 0f;
        f.frictionCombine = PhysicsMaterialCombine.Minimum;
        noFriction = Replace(f, "Assets/Materials/NoFriction.asset");

        gemMesh = Replace(MakeOctahedron(), "Assets/Meshes/Gem.asset");
        ApplyDesertTextures();
    }

    static Mesh MakeOctahedron()
    {
        Vector3[] p = { new Vector3(0, 1.3f, 0), new Vector3(0, -1.3f, 0), new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1) };
        int[][] faces = { new[] { 0, 4, 2 }, new[] { 0, 3, 4 }, new[] { 0, 5, 3 }, new[] { 0, 2, 5 }, new[] { 1, 2, 4 }, new[] { 1, 4, 3 }, new[] { 1, 3, 5 }, new[] { 1, 5, 2 } };
        List<Vector3> v = new List<Vector3>();
        List<int> t = new List<int>();
        foreach (int[] f in faces)
        {
            int s = v.Count;
            v.Add(p[f[0]]); v.Add(p[f[1]]); v.Add(p[f[2]]);
            t.Add(s); t.Add(s + 1); t.Add(s + 2);
        }
        Mesh m = new Mesh { name = "Gem" };
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    static void SetModel(string path, ModelImporterAnimationType type, bool loop)
    {
        ModelImporter imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) return;
        imp.materialImportMode = path == HeroCombined ? ModelImporterMaterialImportMode.ImportViaMaterialDescription : ModelImporterMaterialImportMode.None;
        imp.animationType = type;
        imp.importAnimation = type != ModelImporterAnimationType.None;
        imp.SaveAndReimport();
        if (loop && type != ModelImporterAnimationType.None)
        {
            imp = AssetImporter.GetAtPath(path) as ModelImporter;
            ModelImporterClipAnimation[] clips = imp.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].loopTime = true;
                clips[i].loopPose = true;
            }
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
        }
    }

    static AnimationClip LoadClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    static void PrepareHero()
    {
        riggedHero = false;
        useOwnMaterials = false;
        heroSource = null;
        runClip = null;
        heroAvatar = null;
        heroController = null;

        string rigPath = File.Exists(HeroCombined) ? HeroCombined : (File.Exists(HeroRigged) ? HeroRigged : null);
        if (rigPath != null)
        {
            bool combined = rigPath == HeroCombined;
            useOwnMaterials = combined;
            string clipPath = combined ? HeroCombined : RunFbx;
            SetModel(rigPath, ModelImporterAnimationType.Human, true);
            if (!combined && File.Exists(RunFbx)) SetModel(RunFbx, ModelImporterAnimationType.Human, true);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(rigPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isHuman)
            {
                SetModel(rigPath, ModelImporterAnimationType.Generic, true);
                if (!combined && File.Exists(RunFbx)) SetModel(RunFbx, ModelImporterAnimationType.Generic, true);
                avatar = AssetDatabase.LoadAllAssetsAtPath(rigPath).OfType<Avatar>().FirstOrDefault();
            }
            AnimationClip clip = File.Exists(clipPath) ? LoadClip(clipPath) : null;
            if (clip != null)
            {
                heroSource = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
                heroAvatar = avatar;
                runClip = clip;
                riggedHero = heroSource != null;
            }
        }

        if (riggedHero)
        {
            string cp = "Assets/Animations/HeroController.controller";
            if (File.Exists(cp)) AssetDatabase.DeleteAsset(cp);
            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(cp);
            ctrl.AddParameter("RunSpeed", AnimatorControllerParameterType.Float);
            AnimatorControllerParameter[] ps = ctrl.parameters;
            ps[0].defaultFloat = 1f;
            ctrl.parameters = ps;
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState st = sm.AddState("Run");
            st.motion = runClip;
            st.speedParameterActive = true;
            st.speedParameter = "RunSpeed";
            sm.defaultState = st;
            heroController = ctrl;
        }
        else
        {
            SetModel(HeroStatic, ModelImporterAnimationType.None, false);
            heroSource = AssetDatabase.LoadAssetAtPath<GameObject>(HeroStatic);
            if (heroSource == null) throw new System.Exception("Character model not found at " + HeroStatic);
        }

        Material hm = new Material(LitShader());
        hm.name = "Hero";
        Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Character/texture_diffuse.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Character/texture_normal.png");
        if (hm.HasProperty("_BaseMap")) hm.SetTexture("_BaseMap", diffuse);
        if (hm.HasProperty("_MainTex")) hm.SetTexture("_MainTex", diffuse);
        if (hm.HasProperty("_BaseColor")) hm.SetColor("_BaseColor", Color.white);
        if (normal != null)
        {
            if (hm.HasProperty("_BumpMap")) hm.SetTexture("_BumpMap", normal);
            hm.EnableKeyword("_NORMALMAP");
        }
        if (hm.HasProperty("_Smoothness")) hm.SetFloat("_Smoothness", 0.2f);
        if (hm.HasProperty("_Metallic")) hm.SetFloat("_Metallic", 0f);
        mHero = Replace(hm, "Assets/Materials/Hero.mat");
    }

    static Transform BuildHeroModel(Transform parent)
    {
        GameObject holder = new GameObject("Model");
        holder.transform.SetParent(parent, false);
        holder.transform.localRotation = Quaternion.Euler(0f, heroYaw, 0f);

        GameObject inst = Object.Instantiate(heroSource, holder.transform);
        inst.name = "Hero";
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;

        Renderer[] renderers = inst.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (useOwnMaterials) break;
            Material[] mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
            for (int i = 0; i < mats.Length; i++) mats[i] = mHero;
            r.sharedMaterials = mats;
        }

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        float s = 1.8f / Mathf.Max(0.01f, b.size.y);
        Vector3 origin = holder.transform.position;
        inst.transform.localScale = Vector3.one * s;
        inst.transform.position = origin + new Vector3(-(b.center.x - origin.x) * s, -(b.min.y - origin.y) * s, -(b.center.z - origin.z) * s);

        if (riggedHero)
        {
            Animator an = inst.GetComponent<Animator>();
            if (an == null) an = inst.AddComponent<Animator>();
            an.runtimeAnimatorController = heroController;
            if (heroAvatar != null) an.avatar = heroAvatar;
            an.applyRootMotion = false;
            an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        else
        {
            inst.AddComponent<RunBob>();
        }
        return holder.transform;
    }


    static Texture2D Desert(string n) { return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Desert/" + n + ".png"); }

    static void Skin(Material m, string albedo, string normal, Vector2 tiling, Color tint, float bump)
    {
        Texture2D a = Desert(albedo);
        Texture2D n = Desert(normal);
        if (a != null)
        {
            m.SetTexture("_BaseMap", a);
            m.SetTextureScale("_BaseMap", tiling);
            m.SetColor("_BaseColor", tint);
        }
        if (n != null)
        {
            m.SetTexture("_BumpMap", n);
            m.SetTextureScale("_BumpMap", tiling);
            m.SetFloat("_BumpScale", bump);
            m.EnableKeyword("_NORMALMAP");
        }
        EditorUtility.SetDirty(m);
    }

    static void ApplyDesertTextures()
    {
        Skin(mTrack, "sand_albedo", "sand_normal", new Vector2(2f, 13f), new Color(1f, 0.96f, 0.9f), 0.8f);
        Skin(mSand, "sand_albedo", "sand_normal", Vector2.one, new Color(0.92f, 0.84f, 0.72f), 1.2f);
        Skin(mMesa1, "rock_albedo", "rock_normal", new Vector2(1.5f, 3f), new Color(1f, 0.95f, 0.9f), 1f);
        Skin(mMesa2, "rock_albedo", "rock_normal", new Vector2(2f, 4f), new Color(0.85f, 0.75f, 0.7f), 1f);
        Skin(mMesa3, "rock_albedo", "rock_normal", new Vector2(1f, 2.5f), Color.white, 1f);
        Skin(mCrate, "wood_albedo", "wood_normal", Vector2.one, Color.white, 1f);
        Skin(mTrunk, "wood_albedo", "wood_normal", new Vector2(1f, 2f), new Color(0.8f, 0.7f, 0.6f), 1f);
        Skin(mRock, "rock_albedo", "rock_normal", Vector2.one, new Color(0.62f, 0.6f, 0.6f), 1.3f);
        AssetDatabase.SaveAssets();
    }

    static Mesh LoadMesh(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();
    }

    static void LoadEnvironment()
    {
        AssetDatabase.Refresh();
        duneLeft = LoadMesh("Assets/Models/Environment/DuneLeft.obj");
        duneRight = LoadMesh("Assets/Models/Environment/DuneRight.obj");
        rockMeshes = new[] { LoadMesh("Assets/Models/Environment/Rock1.obj"), LoadMesh("Assets/Models/Environment/Rock2.obj"), LoadMesh("Assets/Models/Environment/Rock3.obj") }.Where(m => m != null).ToArray();
    }

    static GameObject MeshPart(string name, Transform parent, Vector3 pos, Vector3 scale, Mesh mesh, Material mat)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return g;
    }

    static void MakeAtmosphere()
    {
        Shader sky = Shader.Find("Skybox/Procedural");
        if (sky != null)
        {
            Material m = new Material(sky);
            m.name = "DesertSky";
            m.SetFloat("_SunSize", 0.05f);
            m.SetFloat("_SunSizeConvergence", 4f);
            m.SetFloat("_AtmosphereThickness", 1.2f);
            m.SetColor("_SkyTint", new Color(0.55f, 0.62f, 0.76f));
            m.SetColor("_GroundColor", new Color(0.82f, 0.68f, 0.48f));
            m.SetFloat("_Exposure", 1.25f);
            skyMat = Replace(m, "Assets/Materials/DesertSky.mat");
        }

        string pp = "Assets/Materials/DesertFX.asset";
        if (File.Exists(pp)) AssetDatabase.DeleteAsset(pp);
        VolumeProfile prof = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(prof, pp);

        Bloom bloom = prof.Add<Bloom>(true);
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(0.7f);
        bloom.scatter.Override(0.65f);
        AssetDatabase.AddObjectToAsset(bloom, prof);

        Tonemapping tone = prof.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);
        AssetDatabase.AddObjectToAsset(tone, prof);

        ColorAdjustments ca = prof.Add<ColorAdjustments>(true);
        ca.postExposure.Override(0.15f);
        ca.contrast.Override(14f);
        ca.saturation.Override(10f);
        AssetDatabase.AddObjectToAsset(ca, prof);

        Vignette vg = prof.Add<Vignette>(true);
        vg.intensity.Override(0.25f);
        vg.smoothness.Override(0.45f);
        AssetDatabase.AddObjectToAsset(vg, prof);

        EditorUtility.SetDirty(prof);
        AssetDatabase.SaveAssets();
        fxProfile = prof;

        RenderPipelineAsset current = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
        UniversalRenderPipelineAsset urp = current as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urp.supportsHDR = true;
            urp.msaaSampleCount = 4;
            urp.shadowDistance = 90f;
            urp.shadowCascadeCount = 3;
            EditorUtility.SetDirty(urp);
        }
    }

    static void ApplyLook(Camera cam, bool useSky)
    {
        if (useSky && skyMat != null)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.skybox = skyMat;
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.68f, 0.92f);
        RenderSettings.ambientEquatorColor = new Color(0.78f, 0.68f, 0.54f);
        RenderSettings.ambientGroundColor = new Color(0.42f, 0.33f, 0.24f);
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        GameObject v = new GameObject("PostFX");
        Volume vol = v.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = fxProfile;
    }

    static void SetRef(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning("Missing field " + field + " on " + target.GetType().Name); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetArray(Object target, string field, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning("Missing field " + field + " on " + target.GetType().Name); return; }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetEnum(Object target, string field, int index)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) return;
        p.enumValueIndex = index;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject BuildPlayerPrefab()
    {
        GameObject root = new GameObject("Player");
        root.tag = "Player";
        root.transform.position = Vector3.zero;
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 1f;
        CapsuleCollider cap = root.AddComponent<CapsuleCollider>();
        cap.radius = 0.35f;
        cap.height = 1.7f;
        cap.center = new Vector3(0f, 0.85f, 0f);
        cap.sharedMaterial = noFriction;
        PlayerMovement pm = root.AddComponent<PlayerMovement>();
        root.AddComponent<PlayerDust>();
        Transform model = BuildHeroModel(root.transform);
        SetRef(pm, "model", model);
        SetRef(pm, "animator", model.GetComponentInChildren<Animator>());
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Player.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = false)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static void CactusVisual(Transform p, float k)
    {
        Prim(PrimitiveType.Capsule, "Trunk", p, new Vector3(0, 1.5f * k, 0), new Vector3(0.7f, 1.5f, 0.7f) * k, mCactus);
        GameObject a = Prim(PrimitiveType.Capsule, "ArmL", p, new Vector3(-0.6f * k, 1.7f * k, 0), new Vector3(0.38f, 0.55f, 0.38f) * k, mCactus);
        a.transform.localRotation = Quaternion.Euler(0, 0, 40);
        GameObject b = Prim(PrimitiveType.Capsule, "ArmR", p, new Vector3(0.6f * k, 1.3f * k, 0), new Vector3(0.38f, 0.5f, 0.38f) * k, mCactus);
        b.transform.localRotation = Quaternion.Euler(0, 0, -40);
    }

    static void TreeVisual(Transform p, float k)
    {
        Prim(PrimitiveType.Cylinder, "Trunk", p, new Vector3(0, 1.2f * k, 0), new Vector3(0.5f, 1.2f, 0.5f) * k, mTrunk);
        GameObject l1 = Prim(PrimitiveType.Sphere, "Leaves1", p, new Vector3(0, 3.0f * k, 0), new Vector3(2.4f, 1.6f, 2.4f) * k, mLeaf);
        l1.transform.localRotation = Quaternion.Euler(0, 30, 0);
        GameObject l2 = Prim(PrimitiveType.Sphere, "Leaves2", p, new Vector3(0, 3.9f * k, 0), new Vector3(1.6f, 1.2f, 1.6f) * k, mLeaf);
        l2.transform.localRotation = Quaternion.Euler(0, 60, 0);
    }

    static void RockVisual(Transform p, float k, float yaw)
    {
        int variant = Mathf.Abs((int)yaw) % 3;
        if (rockMeshes != null && rockMeshes.Length > 0)
        {
            Mesh m = rockMeshes[variant % rockMeshes.Length];
            GameObject r = MeshPart("Rock", p, new Vector3(0, -0.05f * k, 0), new Vector3(1.05f, 1.15f, 0.9f) * k, m, mRock);
            r.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            GameObject s2 = MeshPart("RockSmall", p, new Vector3(0.95f * k, -0.05f * k, 0.6f * k), new Vector3(0.45f, 0.5f, 0.4f) * k, rockMeshes[(variant + 1) % rockMeshes.Length], mRock);
            s2.transform.localRotation = Quaternion.Euler(0, yaw + 70, 0);
            return;
        }
        GameObject fallback = Prim(PrimitiveType.Sphere, "Rock", p, new Vector3(0, 0.5f * k, 0), new Vector3(2.1f, 1.2f, 1.8f) * k, mRock);
        fallback.transform.localRotation = Quaternion.Euler(0, yaw, 0);
    }

    static void CrateVisual(Transform p)
    {
        Prim(PrimitiveType.Cube, "Crate", p, new Vector3(0, 0.5f, 0), new Vector3(1.8f, 1f, 1.8f), mCrate);
        Prim(PrimitiveType.Cube, "StrapLow", p, new Vector3(0, 0.2f, 0), new Vector3(1.86f, 0.14f, 1.86f), mCrateDark);
        Prim(PrimitiveType.Cube, "StrapHigh", p, new Vector3(0, 0.8f, 0), new Vector3(1.86f, 0.14f, 1.86f), mCrateDark);
        Prim(PrimitiveType.Cube, "StrapSideA", p, new Vector3(0, 0.5f, 0), new Vector3(0.14f, 1.02f, 1.86f), mCrateDark);
        Prim(PrimitiveType.Cube, "StrapSideB", p, new Vector3(0, 0.5f, 0), new Vector3(1.86f, 1.02f, 0.14f), mCrateDark);
    }

    static GameObject MakeObstacle(string kind, Transform parent, Vector3 pos, System.Random rnd)
    {
        GameObject o = new GameObject(kind);
        o.transform.SetParent(parent, false);
        o.transform.localPosition = pos;
        o.AddComponent<Obstacle>();
        BoxCollider c = o.AddComponent<BoxCollider>();
        c.isTrigger = true;
        switch (kind)
        {
            case "Crate":
                CrateVisual(o.transform);
                c.center = new Vector3(0, 0.5f, 0);
                c.size = new Vector3(1.7f, 0.95f, 1.7f);
                break;
            case "Rock":
                RockVisual(o.transform, 1f, rnd.Next(0, 180));
                c.center = new Vector3(0, 0.5f, 0);
                c.size = new Vector3(1.7f, 1f, 1.5f);
                break;
            case "Cactus":
                CactusVisual(o.transform, 1f);
                c.center = new Vector3(0, 1.5f, 0);
                c.size = new Vector3(1.0f, 3f, 1.0f);
                break;
            default:
                TreeVisual(o.transform, 1f);
                c.center = new Vector3(0, 2.2f, 0);
                c.size = new Vector3(1.4f, 4.4f, 1.4f);
                break;
        }
        return o;
    }

    static void Coin(Transform parent, Vector3 pos)
    {
        GameObject c = new GameObject("Coin");
        c.transform.SetParent(parent, false);
        c.transform.localPosition = pos;
        Collectible col = c.AddComponent<Collectible>();
        SetEnum(col, "kind", 0);
        SphereCollider sc = c.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.7f;
        GameObject v = Prim(PrimitiveType.Cylinder, "CoinVisual", c.transform, Vector3.zero, new Vector3(0.75f, 0.05f, 0.75f), mGold);
        v.transform.localRotation = Quaternion.Euler(90, 0, 0);
        GameObject rim = Prim(PrimitiveType.Cylinder, "CoinInner", c.transform, Vector3.zero, new Vector3(0.5f, 0.07f, 0.5f), mGold);
        rim.transform.localRotation = Quaternion.Euler(90, 0, 0);
    }

    static void Gem(Transform parent, Vector3 pos)
    {
        GameObject g = new GameObject("Gem");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        Collectible col = g.AddComponent<Collectible>();
        SetEnum(col, "kind", 1);
        SphereCollider sc = g.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.8f;
        GameObject v = new GameObject("GemVisual");
        v.transform.SetParent(g.transform, false);
        v.transform.localScale = Vector3.one * 0.5f;
        v.AddComponent<MeshFilter>().sharedMesh = gemMesh;
        v.AddComponent<MeshRenderer>().sharedMaterial = mGem;
    }

    static void Mesa(Transform parent, System.Random rnd, float x, float z)
    {
        Material[] mats = { mMesa1, mMesa2, mMesa3 };
        float w = 12f + (float)rnd.NextDouble() * 12f;
        float h1 = 8f + (float)rnd.NextDouble() * 12f;
        GameObject m = new GameObject("Mesa");
        m.transform.SetParent(parent, false);
        m.transform.localPosition = new Vector3(x, 0, z);
        m.transform.localRotation = Quaternion.Euler(0, rnd.Next(0, 90), 0);
        Prim(PrimitiveType.Cube, "Base", m.transform, new Vector3(0, h1 / 2f - 0.5f, 0), new Vector3(w, h1, w * 0.9f), mats[rnd.Next(0, 3)]);
        float h2 = 4f + (float)rnd.NextDouble() * 8f;
        Prim(PrimitiveType.Cube, "Mid", m.transform, new Vector3(0.5f, h1 + h2 / 2f - 0.5f, 0), new Vector3(w * 0.68f, h2, w * 0.6f), mats[rnd.Next(0, 3)]);
        if (rnd.NextDouble() > 0.4)
        {
            float h3 = 3f + (float)rnd.NextDouble() * 5f;
            Prim(PrimitiveType.Cube, "Top", m.transform, new Vector3(-0.4f, h1 + h2 + h3 / 2f - 0.5f, 0.2f), new Vector3(w * 0.36f, h3, w * 0.34f), mats[rnd.Next(0, 3)]);
        }
    }

    static void Decor(Transform parent, System.Random rnd, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float side = rnd.NextDouble() > 0.5 ? 1f : -1f;
            float x = side * (TW / 2f + 3f + (float)rnd.NextDouble() * 14f);
            float z = (float)rnd.NextDouble() * L;
            double pick = rnd.NextDouble();
            GameObject d = new GameObject("Decor");
            d.transform.SetParent(parent, false);
            d.transform.localPosition = new Vector3(x, 0, z);
            float k = 0.8f + (float)rnd.NextDouble() * 0.8f;
            if (pick < 0.45) CactusVisual(d.transform, k);
            else if (pick < 0.75) RockVisual(d.transform, k, rnd.Next(0, 180));
            else TreeVisual(d.transform, k);
        }
    }

    static GameObject BuildSegmentObject(string name, int seed, bool empty)
    {
        System.Random rnd = new System.Random(seed);
        GameObject root = new GameObject(name);
        GameObject gnd = Prim(PrimitiveType.Cube, "Ground", root.transform, new Vector3(0, -0.5f, L / 2f), new Vector3(TW, 1f, L), mTrack, true);
        Object.DestroyImmediate(gnd.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(gnd.GetComponent<MeshFilter>());
        Prim(PrimitiveType.Plane, "TrackSurface", root.transform, new Vector3(0, 0.002f, L / 2f), new Vector3(TW / 10f, 1f, L / 10f), mTrack);
        for (int s = -1; s <= 1; s += 2)
        {
            if (duneLeft != null && duneRight != null) MeshPart("Dunes", root.transform, Vector3.zero, Vector3.one, s < 0 ? duneLeft : duneRight, mSand);
            else Prim(PrimitiveType.Cube, "Floor", root.transform, new Vector3(s * (TW / 2f + 40f), -0.55f, L / 2f), new Vector3(80f, 1f, L), mSand);
            Prim(PrimitiveType.Cube, "Kerb", root.transform, new Vector3(s * (TW / 2f + 0.2f), 0.05f, L / 2f), new Vector3(0.4f, 0.2f, L), mLine);
        }
        foreach (float lx in new[] { -1.5f, 1.5f })
            Prim(PrimitiveType.Cube, "LaneLine", root.transform, new Vector3(lx, 0.01f, L / 2f), new Vector3(0.08f, 0.02f, L), mLine);

        for (int i = 0; i < 3; i++) Mesa(root.transform, rnd, -(26f + (float)rnd.NextDouble() * 34f), (float)rnd.NextDouble() * L);
        for (int i = 0; i < 3; i++) Mesa(root.transform, rnd, 26f + (float)rnd.NextDouble() * 34f, (float)rnd.NextDouble() * L);
        Decor(root.transform, rnd, 16);

        if (name == "StartSegment")
        {
            Prim(PrimitiveType.Cube, "GateL", root.transform, new Vector3(-TW / 2f - 0.6f, 3f, L / 2f + 14f), new Vector3(1f, 6f, 1f), mGate);
            Prim(PrimitiveType.Cube, "GateR", root.transform, new Vector3(TW / 2f + 0.6f, 3f, L / 2f + 14f), new Vector3(1f, 6f, 1f), mGate);
            Prim(PrimitiveType.Cube, "GateBeam", root.transform, new Vector3(0, 6.3f, L / 2f + 14f), new Vector3(TW + 3.2f, 1.2f, 1f), mGate);
        }

        if (empty) return root;

        float[] lanes = { -3f, 0f, 3f };
        for (int row = 0; row < 5; row++)
        {
            float z = 12f + row * 10f + (float)rnd.NextDouble() * 2f;
            List<int> free = new List<int> { 0, 1, 2 };
            int pat = rnd.Next(0, 5);
            int a = rnd.Next(0, 3);
            if (pat <= 1)
            {
                string kind = rnd.NextDouble() > 0.5 ? "Crate" : "Rock";
                MakeObstacle(kind, root.transform, new Vector3(lanes[a], 0, z), rnd);
                free.Remove(a);
                for (int i = -2; i <= 2; i++)
                {
                    float dz = i * 1.5f;
                    float y = 1.0f + 1.0f * (1f - (dz / 3f) * (dz / 3f));
                    Coin(root.transform, new Vector3(lanes[a], y, z + dz));
                }
            }
            else if (pat == 2)
            {
                MakeObstacle(rnd.NextDouble() > 0.5 ? "Cactus" : "Tree", root.transform, new Vector3(lanes[a], 0, z), rnd);
                free.Remove(a);
                int lane = free[rnd.Next(0, free.Count)];
                for (int i = -1; i <= 1; i++) Coin(root.transform, new Vector3(lanes[lane], 1f, z + i * 2.2f));
            }
            else if (pat == 3)
            {
                int b = (a + 1 + rnd.Next(0, 2)) % 3;
                MakeObstacle("Crate", root.transform, new Vector3(lanes[a], 0, z), rnd);
                MakeObstacle(rnd.NextDouble() > 0.5 ? "Cactus" : "Tree", root.transform, new Vector3(lanes[b], 0, z), rnd);
                free.Remove(a);
                free.Remove(b);
                int lane = free[0];
                for (int i = -2; i <= 2; i++) Coin(root.transform, new Vector3(lanes[lane], 1f, z + i * 2f));
            }
            else
            {
                for (int i = -2; i <= 3; i++) Coin(root.transform, new Vector3(lanes[a], 1f, z + i * 2f));
                if (rnd.NextDouble() > 0.5) Gem(root.transform, new Vector3(lanes[(a + 1) % 3], 1.2f, z));
            }
            if (pat <= 3 && rnd.NextDouble() < 0.3 && free.Count > 0)
                Gem(root.transform, new Vector3(lanes[free[rnd.Next(0, free.Count)]], 1.2f, z + 5f));
        }
        return root;
    }

    static GameObject[] BuildSegments(out GameObject start)
    {
        GameObject s = BuildSegmentObject("StartSegment", 11, true);
        start = PrefabUtility.SaveAsPrefabAsset(s, "Assets/Prefabs/StartSegment.prefab");
        Object.DestroyImmediate(s);
        List<GameObject> list = new List<GameObject>();
        for (int i = 0; i < 6; i++)
        {
            GameObject g = BuildSegmentObject("DesertSegment" + (i + 1), 100 + i * 17, false);
            list.Add(PrefabUtility.SaveAsPrefabAsset(g, "Assets/Prefabs/DesertSegment" + (i + 1) + ".prefab"));
            Object.DestroyImmediate(g);
        }
        return list.ToArray();
    }

    static AudioClip Clip(string n) { return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + n + ".wav"); }

    static Camera MakeCamera(Vector3 pos, Vector3 euler, Color bg)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bg;
        cam.nearClipPlane = 0.2f;
        cam.farClipPlane = 280f;
        cam.fieldOfView = 62f;
        go.AddComponent<AudioListener>();
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
        return cam;
    }

    static void MakeLightAndEnvironment(bool fog)
    {
        GameObject l = new GameObject("Sun");
        Light light = l.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.92f, 0.78f);
        light.intensity = 1.5f;
        light.shadows = LightShadows.Soft;
        l.transform.rotation = Quaternion.Euler(40f, -38f, 0f);
        RenderSettings.sun = light;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.55f, 0.5f);
        RenderSettings.fog = fog;
        if (fog)
        {
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.82f, 0.76f, 0.66f);
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 235f;
        }
    }

    static void MakeEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    static GameObject MakeAudio()
    {
        GameObject a = new GameObject("AudioManager");
        AudioManager am = a.AddComponent<AudioManager>();
        SetRef(am, "coin", Clip("coin"));
        SetRef(am, "gem", Clip("gem"));
        SetRef(am, "crash", Clip("crash"));
        SetRef(am, "click", Clip("click"));
        SetRef(am, "jump", Clip("jump"));
        SetRef(am, "wind", Clip("wind"));
        SetRef(am, "music", Clip("music"));
        return a;
    }

    static GameObject MakeCanvas(string name, int sort)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sort;
        CanvasScaler s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        s.matchWidthOrHeight = 0.5f;
        return go;
    }

    static void MakeFader()
    {
        GameObject c = MakeCanvas("Fader", 500);
        CanvasGroup g = c.AddComponent<CanvasGroup>();
        Image im = new GameObject("Black", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        im.transform.SetParent(c.transform, false);
        im.color = Color.black;
        im.raycastTarget = false;
        Stretch(im.rectTransform);
        ScreenFader f = c.AddComponent<ScreenFader>();
        SetRef(f, "group", g);
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        RectTransform r = g.GetComponent<RectTransform>();
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = anchor;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    static Image Box(Transform p, string name, Color c, Vector2 anchor, Vector2 pos, Vector2 size, bool circle = false)
    {
        RectTransform r = NewRect(name, p, anchor, pos, size);
        Image im = r.gameObject.AddComponent<Image>();
        im.color = c;
        im.sprite = circle ? knobSprite : uiSprite;
        im.type = circle ? Image.Type.Simple : Image.Type.Sliced;
        im.raycastTarget = false;
        return im;
    }

    static TextMeshProUGUI Label(Transform p, string name, string text, float size, Color c, Vector2 anchor, Vector2 pos, Vector2 box, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        RectTransform r = NewRect(name, p, anchor, pos, box);
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = c;
        t.alignment = align;
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        return t;
    }

    static Button Btn(Transform p, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color c, float fs, out TextMeshProUGUI text)
    {
        Image im = Box(p, label + " Button", c, anchor, pos, size);
        im.raycastTarget = true;
        Button b = im.gameObject.AddComponent<Button>();
        b.targetGraphic = im;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.96f, 0.82f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        b.colors = cb;
        text = Label(b.transform, "Text", label, fs, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return b;
    }

    static Button Btn(Transform p, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color c, float fs)
    {
        TextMeshProUGUI t;
        return Btn(p, label, anchor, pos, size, c, fs, out t);
    }

    static TextMeshProUGUI HudRow(Transform p, int index, Color icon)
    {
        Image panel = Box(p, "HudRow" + index, new Color(0f, 0f, 0f, 0.5f), new Vector2(0, 1), new Vector2(30, -30 - index * 72), new Vector2(300, 62));
        Box(panel.transform, "Icon", icon, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(38, 38), true);
        return Label(panel.transform, "Value", "0", 38, Color.white, new Vector2(0, 0.5f), new Vector2(70, 0), new Vector2(220, 56), TextAlignmentOptions.Left);
    }

    static Color Hex(string h)
    {
        Color c;
        ColorUtility.TryParseHtmlString(h, out c);
        return c;
    }

    static UnityEngine.SceneManagement.Scene NewScene()
    {
        return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static string SaveScene(UnityEngine.SceneManagement.Scene scene, string name)
    {
        string path = "Assets/Scenes/" + name + ".unity";
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }

    static string BuildMainMenu(GameObject startSegment)
    {
        UnityEngine.SceneManagement.Scene scene = NewScene();
        MakeLightAndEnvironment(false);
        Camera cam = MakeCamera(new Vector3(0, 1.5f, 4.6f), new Vector3(8, 180, 0), new Color(0.56f, 0.76f, 0.95f));
        ApplyLook(cam, true);
        GameObject seg = (GameObject)PrefabUtility.InstantiatePrefab(startSegment);
        seg.transform.position = new Vector3(0, 0, -30f);

        GameObject heroRoot = new GameObject("MenuHero");
        Transform model = BuildHeroModel(heroRoot.transform);
        model.localRotation = Quaternion.Euler(0, heroYaw + 180f, 0);
        MenuCameraSway sway = cam.gameObject.AddComponent<MenuCameraSway>();
        SetRef(sway, "focus", heroRoot.transform);
        heroRoot.transform.position = Vector3.zero;

        MakeEventSystem();
        MakeAudio();
        GameObject cv = MakeCanvas("Canvas", 0);
        MenuControl mc = new GameObject("MenuControl").AddComponent<MenuControl>();

        Label(cv.transform, "Title", "MIRAGE RUN", 150, Hex("#FFF1D0"), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(1400, 190));
        Label(cv.transform, "Subtitle", "ENDLESS DESERT RUNNER", 54, Hex("#FFC857"), new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(1200, 80));

        Button start = Btn(cv.transform, "START GAME", new Vector2(0.5f, 0), new Vector2(0, 250), new Vector2(520, 120), Hex("#3FA34D"), 54);
        UnityEventTools.AddIntPersistentListener(start.onClick, mc.LoadScene, 1);
        TextMeshProUGUI soundText;
        Button sound = Btn(cv.transform, "SOUND: ON", new Vector2(0.5f, 0), new Vector2(-170, 110), new Vector2(330, 90), Hex("#D9692B"), 36, out soundText);
        UnityEventTools.AddVoidPersistentListener(sound.onClick, mc.ToggleSound);
        Button quit = Btn(cv.transform, "QUIT", new Vector2(0.5f, 0), new Vector2(170, 110), new Vector2(330, 90), Hex("#B5452E"), 36);
        UnityEventTools.AddVoidPersistentListener(quit.onClick, mc.Quit);

        Image panel = Box(cv.transform, "Stats", new Color(0, 0, 0, 0.5f), new Vector2(0, 0), new Vector2(40, 40), new Vector2(360, 230));
        Label(panel.transform, "Heading", "SAVED PROGRESS", 28, Hex("#FFC857"), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(340, 40));
        TextMeshProUGUI gems = RowStat(panel.transform, 0, Hex("#E0192A"));
        TextMeshProUGUI coins = RowStat(panel.transform, 1, Hex("#F5C020"));
        TextMeshProUGUI best = RowStat(panel.transform, 2, Hex("#3CC850"));

        SetRef(mc, "coinText", coins);
        SetRef(mc, "gemText", gems);
        SetRef(mc, "bestText", best);
        SetRef(mc, "soundText", soundText);
        MakeFader();
        return SaveScene(scene, "MainMenu");
    }

    static TextMeshProUGUI RowStat(Transform p, int i, Color icon)
    {
        RectTransform row = NewRect("Row" + i, p, new Vector2(0, 1), new Vector2(20, -62 - i * 55), new Vector2(320, 48));
        Box(row, "Icon", icon, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(30, 30), true);
        return Label(row, "Value", "0", 34, Color.white, new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(260, 48), TextAlignmentOptions.Left);
    }

    static string BuildStageSelect()
    {
        UnityEngine.SceneManagement.Scene scene = NewScene();
        MakeLightAndEnvironment(false);
        ApplyLook(MakeCamera(new Vector3(0, 3.6f, -8.5f), new Vector3(16, 0, 0), Hex("#0B0806")), false);

        GameObject island = new GameObject("Island");
        island.transform.position = new Vector3(0, 0.2f, 0);
        Prim(PrimitiveType.Cube, "Block", island.transform, new Vector3(0, 0, 0), new Vector3(4f, 3f, 4f), mTrack);
        GameObject t = new GameObject("Tree");
        t.transform.SetParent(island.transform, false);
        t.transform.localPosition = new Vector3(0.7f, 1.5f, 0.5f);
        TreeVisual(t.transform, 0.45f);
        GameObject r = new GameObject("Rock");
        r.transform.SetParent(island.transform, false);
        r.transform.localPosition = new Vector3(-1f, 1.5f, -0.6f);
        RockVisual(r.transform, 0.4f, 25);
        GameObject c = new GameObject("Cactus");
        c.transform.SetParent(island.transform, false);
        c.transform.localPosition = new Vector3(-0.2f, 1.5f, 1.2f);
        CactusVisual(c.transform, 0.3f);
        Spin spin = island.AddComponent<Spin>();

        MakeEventSystem();
        MakeAudio();
        GameObject cv = MakeCanvas("Canvas", 0);
        MenuControl mc = new GameObject("MenuControl").AddComponent<MenuControl>();
        Label(cv.transform, "Heading", "SELECT STAGE", 60, Hex("#FFC857"), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(1200, 90));
        Label(cv.transform, "Stage", "DESERT RUN", 110, Color.white, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1400, 150));
        Button play = Btn(cv.transform, "PLAY", new Vector2(0.5f, 0), new Vector2(120, 80), new Vector2(380, 110), Hex("#3FA34D"), 54);
        UnityEventTools.AddIntPersistentListener(play.onClick, mc.LoadScene, 2);
        Button back = Btn(cv.transform, "BACK", new Vector2(0.5f, 0), new Vector2(-300, 80), new Vector2(300, 110), Hex("#8C6A4F"), 46);
        UnityEventTools.AddIntPersistentListener(back.onClick, mc.LoadScene, 0);
        MakeFader();
        return SaveScene(scene, "StageSelect");
    }

    static void KeyCap(Transform p, string key, string action, Vector2 pos)
    {
        Image cap = Box(p, key + " Key", Hex("#2B1A10"), new Vector2(0.5f, 0.5f), pos, new Vector2(400, 150));
        Label(cap.transform, "Key", key, 64, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 150));
        Label(p, action, action, 46, Hex("#FFC857"), new Vector2(0.5f, 0.5f), pos + new Vector2(0, -130), new Vector2(520, 70));
    }

    static string BuildInformation()
    {
        UnityEngine.SceneManagement.Scene scene = NewScene();
        MakeLightAndEnvironment(false);
        MakeCamera(new Vector3(0, 1, -10), Vector3.zero, Hex("#1B120C"));
        MakeEventSystem();
        MakeAudio();
        GameObject cv = MakeCanvas("Canvas", 0);
        Label(cv.transform, "Heading", "HOW TO PLAY", 90, Hex("#FFF1D0"), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(1400, 130));
        KeyCap(cv.transform, "A  /  LEFT", "MOVE LEFT", new Vector2(-520, 80));
        KeyCap(cv.transform, "D  /  RIGHT", "MOVE RIGHT", new Vector2(0, 80));
        KeyCap(cv.transform, "SPACE  /  UP", "JUMP", new Vector2(520, 80));
        Label(cv.transform, "Tips", "Collect coins and gems. Dodge cacti and trees, jump over rocks and crates.\nThe desert speeds up the longer you run.   P or Esc to pause.   M to mute.", 38, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, -250), new Vector2(1700, 150));
        Label(cv.transform, "Loading", "LOADING", 40, Hex("#FFC857"), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(500, 60));
        Image track = Box(cv.transform, "BarTrack", new Color(1, 1, 1, 0.2f), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(700, 20));
        Image fill = Box(track.transform, "BarFill", Hex("#FFC857"), new Vector2(0, 0.5f), Vector2.zero, new Vector2(700, 20));
        fill.rectTransform.pivot = new Vector2(0, 0.5f);
        AutoLoadScene al = new GameObject("AutoLoad").AddComponent<AutoLoadScene>();
        SetRef(al, "bar", fill.rectTransform);
        MakeFader();
        return SaveScene(scene, "Information");
    }

    static string BuildGamePlay(GameObject playerPrefab, GameObject startSegment, GameObject[] segments)
    {
        UnityEngine.SceneManagement.Scene scene = NewScene();
        MakeLightAndEnvironment(true);
        Camera cam = MakeCamera(new Vector3(0, 3.4f, -7.5f), new Vector3(12, 0, 0), new Color(0.55f, 0.76f, 0.96f));
        ApplyLook(cam, true);

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = Vector3.zero;
        CameraFollow cf = cam.gameObject.AddComponent<CameraFollow>();
        SetRef(cf, "target", player.transform);

        GameObject gen = new GameObject("SegmentGenerator");
        SegmentGenerator sg = gen.AddComponent<SegmentGenerator>();
        SetRef(sg, "startSegment", startSegment);
        SetArray(sg, "segments", segments);
        SetRef(sg, "player", player.transform);

        MakeEventSystem();
        MakeAudio();

        GameObject cv = MakeCanvas("Canvas", 0);
        TextMeshProUGUI gemText = HudRow(cv.transform, 0, Hex("#E0192A"));
        TextMeshProUGUI coinText = HudRow(cv.transform, 1, Hex("#F5C020"));
        TextMeshProUGUI distText = HudRow(cv.transform, 2, Hex("#3CC850"));
        Image scorePanel = Box(cv.transform, "ScorePanel", new Color(0, 0, 0, 0.5f), new Vector2(1, 1), new Vector2(-150, -30), new Vector2(300, 90));
        Label(scorePanel.transform, "ScoreLabel", "SCORE", 24, Hex("#FFC857"), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(280, 30));
        TextMeshProUGUI scoreText = Label(scorePanel.transform, "ScoreValue", "0", 46, Color.white, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(280, 56));

        GameObject mli = new GameObject("MasterControl");
        MasterLevelInfo info = mli.AddComponent<MasterLevelInfo>();
        SetRef(info, "coinText", coinText);
        SetRef(info, "gemText", gemText);
        SetRef(info, "distanceText", distText);
        SetRef(info, "scoreText", scoreText);

        GameManager gm = new GameObject("GameManager").AddComponent<GameManager>();
        TextMeshProUGUI countdown = Label(cv.transform, "Countdown", "3", 260, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(900, 320));

        Button pause = Btn(cv.transform, "II", new Vector2(1, 1), new Vector2(-30, -140), new Vector2(110, 90), new Color(0, 0, 0, 0.5f), 48);
        UnityEventTools.AddVoidPersistentListener(pause.onClick, gm.Pause);

        GameObject pausePanel = Dim(cv.transform, "PausePanel");
        Label(pausePanel.transform, "Title", "PAUSED", 120, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(1000, 160));
        Button resume = Btn(pausePanel.transform, "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(520, 100), Hex("#3FA34D"), 46);
        UnityEventTools.AddVoidPersistentListener(resume.onClick, gm.Resume);
        Button restart = Btn(pausePanel.transform, "RESTART", new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(520, 100), Hex("#D9692B"), 46);
        UnityEventTools.AddVoidPersistentListener(restart.onClick, gm.Retry);
        TextMeshProUGUI soundText;
        Button snd = Btn(pausePanel.transform, "SOUND: ON", new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(520, 100), Hex("#3F7CA8"), 46, out soundText);
        UnityEventTools.AddVoidPersistentListener(snd.onClick, gm.ToggleMute);
        Button menuP = Btn(pausePanel.transform, "MAIN MENU", new Vector2(0.5f, 0.5f), new Vector2(0, -230), new Vector2(520, 100), Hex("#B5452E"), 46);
        UnityEventTools.AddVoidPersistentListener(menuP.onClick, gm.MainMenu);

        GameObject overPanel = Dim(cv.transform, "GameOverPanel");
        Label(overPanel.transform, "Title", "GAME OVER", 130, Hex("#FF4A3D"), new Vector2(0.5f, 0.5f), new Vector2(0, 340), new Vector2(1300, 170));
        TextMeshProUGUI result = Label(overPanel.transform, "Result", "", 56, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(900, 330));
        TextMeshProUGUI best = Label(overPanel.transform, "Best", "", 50, Hex("#FFC857"), new Vector2(0.5f, 0.5f), new Vector2(0, -75), new Vector2(900, 70));
        TextMeshProUGUI newBest = Label(overPanel.transform, "NewBest", "NEW BEST!", 60, Hex("#3CC850"), new Vector2(0.5f, 0.5f), new Vector2(0, -145), new Vector2(900, 80));
        Button retry = Btn(overPanel.transform, "RETRY", new Vector2(0.5f, 0.5f), new Vector2(-250, -290), new Vector2(440, 110), Hex("#E8A920"), 54);
        UnityEventTools.AddVoidPersistentListener(retry.onClick, gm.Retry);
        Button menuO = Btn(overPanel.transform, "MAIN MENU", new Vector2(0.5f, 0.5f), new Vector2(250, -290), new Vector2(440, 110), Hex("#2F6FE0"), 46);
        UnityEventTools.AddVoidPersistentListener(menuO.onClick, gm.MainMenu);

        SetRef(gm, "player", player.GetComponent<PlayerMovement>());
        SetRef(gm, "countdownText", countdown);
        SetRef(gm, "pausePanel", pausePanel);
        SetRef(gm, "gameOverPanel", overPanel);
        SetRef(gm, "resultText", result);
        SetRef(gm, "bestText", best);
        SetRef(gm, "newBestText", newBest);
        SetRef(gm, "soundButtonText", soundText);
        SetRef(gm, "particleMaterial", mParticle);

        MakeFader();
        return SaveScene(scene, "GamePlay");
    }

    static GameObject Dim(Transform parent, string name)
    {
        GameObject g = new GameObject(name, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(parent, false);
        Stretch(g.GetComponent<RectTransform>());
        Image im = g.GetComponent<Image>();
        im.color = new Color(0f, 0f, 0f, 0.72f);
        return g;
    }
}
