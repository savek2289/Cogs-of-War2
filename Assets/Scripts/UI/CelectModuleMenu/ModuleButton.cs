using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);

        switch (state)
        {
            case SelectionState.Normal:
                CurrentState = ButtonState.Normal;
                break;
            case SelectionState.Highlighted:
                CurrentState = ButtonState.Highlighted;
                break;
            case SelectionState.Pressed:
                CurrentState = ButtonState.Pressed;
                break;
            case SelectionState.Selected:
                CurrentState = ButtonState.Selected;
                break;
            case SelectionState.Disabled:
                CurrentState = ButtonState.Disabled;
                break;
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (characteristics == null)
        {
            characteristics = PlayerCharacteristics.Instance;
        }

        if (pendingButton != null && pendingButton != this)
        {
            pendingButton.ResetConfirmation();
        }

        if (!firstPressReceived)
        {
            firstPressReceived = true;
            IsAwaitingConfirmation = true;
            pendingButton = this; 

            if (TryGetComponent<UIModule>(out UIModule module))
            {
                Dictionary<string, object> values = module.GetValues();

                foreach (var pair in values)
                {
                    string key = pair.Key;
                    if (pair.Value is int intValue)
                    {
                        characteristics.SetChanges(key, intValue);
                    }
                }
            }
            characteristics.SetNeedToReset(true);

            DoStateTransition(SelectionState.Selected, false);
        }
        else
        {
            IsAwaitingConfirmation = false;
            firstPressReceived = false;

            if (pendingButton == this)
                pendingButton = null;

            base.OnPointerClick(eventData);
            characteristics.SetCelectedModule(this);
            characteristics.ApplyChanges();

            DoStateTransition(SelectionState.Normal, false);
        }
    }

    public void ResetConfirmation()
    {
        firstPressReceived = false;
        IsAwaitingConfirmation = false;

        if (pendingButton == this)
            pendingButton = null;

        DoStateTransition(SelectionState.Normal, false);
    }

    protected override void OnDestroy()
    {
        if (pendingButton == this)
        {
            pendingButton = null;
        }
        base.OnDestroy();
    }
}