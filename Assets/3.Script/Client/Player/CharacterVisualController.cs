using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class CharacterVisualController : MonoBehaviour
{
    private const string WarriorPrefabPath = "Assets/DoubleL/Model/Armature (1).prefab";
    private const string WarriorIdleClipPath = "Assets/DoubleL/Demo/Anim/OneHand_Up_Idle.anim";
    private const string WarriorRunClipPath = "Assets/DoubleL/Demo/Anim/OneHand_Up_Run_F_InPlace.anim";
    private const string WarriorAttackClipPath = "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_1_InPlace.anim";

    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip runClip;
    [SerializeField] private AnimationClip attackClip;
    [SerializeField] private float visualScale = 1.0f;

    private PlayableGraph graph;
    private AnimationClipPlayable currentPlayable;
    private AnimationClip currentClip;
    private bool currentLoop;
    private bool isAttackPlaying;
    private float attackEndTime;
    private QuarterViewPlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<QuarterViewPlayerController>();
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        if (isAttackPlaying)
        {
            if (Time.time < attackEndTime)
            {
                return;
            }

            isAttackPlaying = false;
        }

        bool isMoving = playerController != null && playerController.IsMoving;
        PlayLoop(isMoving ? runClip : idleClip);
        UpdateLoopTime();
    }

    private void OnDestroy()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }

    // 선택된 직업에 맞는 시각 모델을 붙입니다. 현재는 전사만 DoubleL 원핸드 세트를 사용합니다.
    public bool ApplyVisual(ClassType classType)
    {
        if (classType != ClassType.Warrior)
        {
            ClearVisual();
            return false;
        }

        if (visualRoot == null)
        {
            GameObject visualObject = LoadWarriorPrefab();
            if (visualObject == null)
            {
                Debug.LogWarning("[CharacterVisualController] Warrior visual prefab을 찾지 못했습니다.");
                return false;
            }

            visualRoot = visualObject.transform;
            visualRoot.SetParent(transform, false);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one * visualScale;
        }

        animator = visualRoot.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = visualRoot.gameObject.AddComponent<Animator>();
        }

        LoadWarriorClips();
        HideRootRenderer();
        PlayLoop(idleClip);
        return true;
    }

    // 스킬 버튼이 눌렸을 때 공격 애니메이션을 1회 재생합니다.
    public void PlayAttack()
    {
        if (animator == null || attackClip == null)
        {
            return;
        }

        PlayClip(attackClip, false);
        isAttackPlaying = true;
        attackEndTime = Time.time + Mathf.Max(0.15f, attackClip.length);
    }

    private void PlayLoop(AnimationClip clip)
    {
        PlayClip(clip, true);
    }

    // Playables로 클립을 직접 재생해 Animator Controller 없이도 에셋 애니메이션을 확인합니다.
    private void PlayClip(AnimationClip clip, bool loop)
    {
        if (clip == null || animator == null || currentClip == clip)
        {
            return;
        }

        if (graph.IsValid())
        {
            graph.Destroy();
        }

        graph = PlayableGraph.Create($"CharacterVisual_{clip.name}");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        currentPlayable = AnimationClipPlayable.Create(graph, clip);
        currentPlayable.SetApplyFootIK(false);
        currentPlayable.SetSpeed(1.0);

        if (!loop)
        {
            currentPlayable.SetDuration(clip.length);
        }

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        output.SetSourcePlayable(currentPlayable);
        graph.Play();
        currentClip = clip;
        currentLoop = loop;
    }

    private void UpdateLoopTime()
    {
        if (!currentLoop || currentClip == null || !currentPlayable.IsValid() || currentClip.length <= 0.01f)
        {
            return;
        }

        double currentTime = currentPlayable.GetTime();
        if (currentTime > currentClip.length)
        {
            currentPlayable.SetTime(currentTime % currentClip.length);
        }
    }

    private void ClearVisual()
    {
        if (visualRoot != null)
        {
            Destroy(visualRoot.gameObject);
            visualRoot = null;
        }

        animator = null;
        currentClip = null;
    }

    private void HideRootRenderer()
    {
        Renderer rootRenderer = GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }
    }

    private void LoadWarriorClips()
    {
        if (idleClip == null)
        {
            idleClip = LoadClip(WarriorIdleClipPath);
        }

        if (runClip == null)
        {
            runClip = LoadClip(WarriorRunClipPath);
        }

        if (attackClip == null)
        {
            attackClip = LoadClip(WarriorAttackClipPath);
        }
    }

    private GameObject LoadWarriorPrefab()
    {
#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorPrefabPath);
        if (prefab != null)
        {
            return Instantiate(prefab);
        }
#endif
        return null;
    }

    private AnimationClip LoadClip(string assetPath)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
#else
        return null;
#endif
    }
}
