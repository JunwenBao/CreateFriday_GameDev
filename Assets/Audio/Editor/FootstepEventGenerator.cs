using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// 在 Project 窗口选中走/跑等 Humanoid 动画 FBX，右键 Audio > Generate Footstep Events，
// 会在 Humanoid Avatar 上逐帧采样片段，找出每只脚落地的时间，写入 FootstepAudio.PlayFootstep 动画事件。
// 生成后可在 FBX Inspector 的 Animation > Events 里预览并微调
public static class FootstepEventGenerator
{
    private const string MenuPath = "Assets/Audio/Generate Footstep Events";

    // 脚踝高度降到 (最低点 + 振幅 × 该比例) 以下视为落地；越小越接近整只脚踩实，越大越接近脚跟刚触地
    private const float ContactRatio = 0.15f;
    // 脚踝高度超过 (最低点 + 振幅 × 该比例) 才视为抬脚，避免一次落地被识别成多次
    private const float LiftRatio = 0.5f;
    // 脚踝高度振幅小于该值（米）的片段视为没有明显迈步，不生成事件
    private const float MinStepAmplitude = 0.02f;
    private const float SampleRate = 240f;

    [MenuItem(MenuPath, true)]
    private static bool ValidateGenerate()
    {
        return GetSelectedModelImporters().Any();
    }

    [MenuItem(MenuPath, false, 1000)]
    private static void Generate()
    {
        foreach (var importer in GetSelectedModelImporters().ToList())
        {
            GenerateForModel(importer);
        }
    }

    private static IEnumerable<ModelImporter> GetSelectedModelImporters()
    {
        return Selection.assetGUIDs
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetImporter.GetAtPath(path) as ModelImporter)
            .Where(importer => importer != null && importer.importAnimation && importer.animationType == ModelImporterAnimationType.Human);
    }

    private static void GenerateForModel(ModelImporter importer)
    {
        string path = importer.assetPath;
        Avatar avatar = importer.sourceAvatar != null
            ? importer.sourceAvatar
            : AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();

        if (avatar == null || !avatar.isHuman)
        {
            Debug.LogWarning($"[Footstep] {path}: 找不到可用的 Humanoid Avatar，已跳过。");
            return;
        }

        var modelPrefab = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(avatar)) as GameObject;
        if (modelPrefab == null)
        {
            Debug.LogWarning($"[Footstep] {path}: 找不到 Avatar 对应的模型，已跳过。");
            return;
        }

        ModelImporterClipAnimation[] clipSettings = importer.clipAnimations;
        if (clipSettings.Length == 0) clipSettings = importer.defaultClipAnimations;

        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .ToArray();

        bool changed = false;
        foreach (var setting in clipSettings)
        {
            AnimationClip clip = clips.FirstOrDefault(c => c.name == setting.name);
            if (clip == null || clip.length <= 0f) continue;

            if (!SampleFootStrikes(modelPrefab, avatar, clip, out List<float> leftTimes, out List<float> rightTimes))
            {
                Debug.LogWarning($"[Footstep] {path} / {clip.name}: 采样失败，已跳过。");
                continue;
            }

            var events = setting.events
                .Where(e => e.functionName != FootstepAudio.AnimationEventFunction)
                .ToList();
            events.AddRange(leftTimes.Select(t => CreateEvent(t, "Left")));
            events.AddRange(rightTimes.Select(t => CreateEvent(t, "Right")));
            setting.events = events.OrderBy(e => e.time).ToArray();
            changed = true;

            string message = $"[Footstep] {clip.name}: 左脚 {FormatTimes(leftTimes, clip.length)} | 右脚 {FormatTimes(rightTimes, clip.length)}";
            if (leftTimes.Count != 1 || rightTimes.Count != 1)
                Debug.LogWarning(message + " —— 每只脚落地次数不是 1，请在 Inspector 中确认。");
            else
                Debug.Log(message);
        }

        if (changed)
        {
            importer.clipAnimations = clipSettings;
            importer.SaveAndReimport();
        }
    }

    private static string FormatTimes(List<float> normalizedTimes, float length)
    {
        if (normalizedTimes.Count == 0) return "无";
        return string.Join(", ", normalizedTimes.Select(t => $"{t * length:0.000}s ({t:0.00})"));
    }

    private static AnimationEvent CreateEvent(float normalizedTime, string foot)
    {
        return new AnimationEvent
        {
            functionName = FootstepAudio.AnimationEventFunction,
            stringParameter = foot,
            // ModelImporter 里的事件时间是归一化时间（0~1）
            time = normalizedTime,
        };
    }

    private static bool SampleFootStrikes(GameObject modelPrefab, Avatar avatar, AnimationClip clip, out List<float> leftTimes, out List<float> rightTimes)
    {
        leftTimes = new List<float>();
        rightTimes = new List<float>();

        var instance = Object.Instantiate(modelPrefab);
        instance.hideFlags = HideFlags.HideAndDontSave;
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        PlayableGraph graph = default;
        try
        {
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = null;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.Rebind();

            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftFoot == null || rightFoot == null) return false;

            graph = PlayableGraph.Create("FootstepEventGenerator");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            var playable = AnimationClipPlayable.Create(graph, clip);
            output.SetSourcePlayable(playable);

            int sampleCount = Mathf.Max(60, Mathf.CeilToInt(clip.length * SampleRate));
            var leftHeights = new float[sampleCount];
            var rightHeights = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                playable.SetTime(clip.length * i / sampleCount);
                graph.Evaluate();
                leftHeights[i] = leftFoot.position.y;
                rightHeights[i] = rightFoot.position.y;
            }

            leftTimes = FindStrikes(leftHeights);
            rightTimes = FindStrikes(rightHeights);
            return true;
        }
        finally
        {
            if (graph.IsValid()) graph.Destroy();
            Object.DestroyImmediate(instance);
        }
    }

    // 把片段当作循环处理：从脚的最高点开始绕一圈，记录每次"抬起后下降穿过落地高度"的时刻（归一化时间）
    private static List<float> FindStrikes(float[] heights)
    {
        var result = new List<float>();
        int n = heights.Length;
        float min = heights.Min();
        float max = heights.Max();
        float amplitude = max - min;
        if (amplitude < MinStepAmplitude) return result;

        float contactHeight = min + amplitude * ContactRatio;
        float liftHeight = min + amplitude * LiftRatio;
        int start = System.Array.IndexOf(heights, max);

        bool lifted = true;
        for (int k = 1; k <= n; k++)
        {
            int i = (start + k) % n;
            int prev = (i - 1 + n) % n;
            float h = heights[i];

            if (h >= liftHeight)
            {
                lifted = true;
            }
            else if (lifted && h <= contactHeight)
            {
                lifted = false;
                float prevH = heights[prev];
                float fraction = prevH > h ? Mathf.Clamp01((prevH - contactHeight) / (prevH - h)) : 1f;
                result.Add(Mathf.Repeat((prev + fraction) / n, 1f));
            }
        }

        result.Sort();
        return result;
    }
}
