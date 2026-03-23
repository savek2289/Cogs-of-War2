using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ModuleButton : Button
{
    private static ModuleButton pendingButton = null;

    public enum ButtonState
    {
        Normal,
        Highlighted,
        Pressed,
        Selected,
        Disabled
    }

    public ButtonState CurrentState { get; private set; }
    public bool IsAwaitingConfirmation { get; private set; }

    private bool firstPressReceived = false;
    private PlayerCharacteristics characteristics;

    protected override void Awake()
    {
        base.Awake();
        characteristics = PlayerCharacteristics.Instance;
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        switch (state)
        {
            case SelectionState.Normal: CurrentState = ButtonState.Normal; break;
            case SelectionState.Highlighted: CurrentState = ButtonState.Highlighted; break;
            case SelectionState.Pressed: CurrentState = ButtonState.Pressed; break;
            case SelectionState.Selected: CurrentState = ButtonState.Selected; break;
            case SelectionState.Disabled: CurrentState = ButtonState.Disabled; break;
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (characteristics == null)
            characteristics = PlayerCharacteristics.Instance;

        if (pendingButton != null && pendingButton != this)
        {
            pendingButton.ResetConfirmation();
        }

        if (!firstPressReceived)
        {
            firstPressReceived = true;
            IsAwaitingConfirmation = true;
            pendingButton = this;

            characteristics.StopAllPreviews();

            if (TryGetComponent<UIModule>(out UIModule module))
            {
                characteristics.PreviewModule(module);
            }

            characteristics.SetNeedToReset(true);
            DoStateTransition(SelectionState.Selected, false);
            base.OnPointerClick(eventData);
        }
        else
        {
            if (!IsAwaitingConfirmation) return;
            StartCoroutine(ConfirmAndSelect(eventData));
        }
    }

    private IEnumerator ConfirmAndSelect(PointerEventData eventData)
    {
        IsAwaitingConfirmation = false;
        firstPressReceived = false;

        if (pendingButton == this)
            pendingButton = null;

        base.OnPointerClick(eventData);

        if (TryGetComponent<UIModule>(out UIModule module))
        {
            characteristics.ApplyChanges(module);
        }
        characteristics.SetCelectedModule(this);
        yield break;
    }

    public void ResetConfirmation()
    {
        if (!firstPressReceived) return;

        firstPressReceived = false;
        IsAwaitingConfirmation = false;

        if (pendingButton == this)
            pendingButton = null;

        if (characteristics != null)
        {
            characteristics.StopAllPreviews();
            characteristics.SetNeedToReset(true);
            characteristics.CancelPreview();
        }

        DoStateTransition(SelectionState.Normal, false);
    }

    protected override void OnDestroy()
    {
        if (pendingButton == this)
            pendingButton = null;
        base.OnDestroy();
    }
}