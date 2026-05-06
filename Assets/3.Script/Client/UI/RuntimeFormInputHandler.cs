using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class RuntimeFormInputHandler : MonoBehaviour, IUpdateSelectedHandler
{
    private Action submitAction;
    private Action tabAction;
    private Action cancelAction;
    private bool pendingSubmitAfterComposition;

    /// 선택된 InputField에서만 Enter/Tab/Esc를 처리하도록 콜백을 연결합니다.
    public void Initialize(Action onSubmit, Action onTab, Action onCancel = null)
    {
        submitAction = onSubmit;
        tabAction = onTab;
        cancelAction = onCancel;
    }

    /// Unity EventSystem이 현재 선택된 UI에만 호출하므로 전체 씬 Update보다 범위가 좁습니다.
    public void OnUpdateSelected(BaseEventData eventData)
    {
        if (pendingSubmitAfterComposition && !HasActiveComposition())
        {
            pendingSubmitAfterComposition = false;
            submitAction?.Invoke();
            return;
        }

        if (IsSubmitPressed())
        {
            submitAction?.Invoke();
            return;
        }

        if (IsTabPressed())
        {
            tabAction?.Invoke();
            return;
        }

        if (IsCancelPressed())
        {
            cancelAction?.Invoke();
        }
    }

    private bool IsSubmitPressed()
    {
        if (HasActiveComposition())
        {
            if (IsSubmitKeyPressed())
            {
                pendingSubmitAfterComposition = true;
            }

            return false;
        }

        return IsSubmitKeyPressed();
    }

    private bool IsSubmitKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    /// 한글 IME 조합 중 Enter는 전송이 아니라 조합 확정에 쓰이므로 폼 제출을 막습니다.
    private bool HasActiveComposition()
    {
        return !string.IsNullOrEmpty(Input.compositionString);
    }

    private bool IsTabPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Tab);
#endif
    }

    private bool IsCancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
