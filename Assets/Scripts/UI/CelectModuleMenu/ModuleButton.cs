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
            // ѕервый клик - предварительный просмотр
            firstPressReceived = true;
            IsAwaitingConfirmation = true;
            pendingButton = this;

            if (TryGetComponent<UIModule>(out UIModule module))
            {
                List<UIModule.Values> values = module.GetValues();

                foreach (var value in values)
                {
                    string key = value.Name;
                    int intValue = Mathf.RoundToInt(value.AddedValue);

                    if (characteristics.HasCharacteristic(key))
                    {
                        characteristics.SetChanges(key, intValue);
                    }
                    else
                    {
                        Debug.LogWarning($"ћодуль {gameObject.name} пытаетс€ изменить характеристику '{key}', которой нет в PlayerCharacteristics!");
                    }
                }
            }

            characteristics.SetNeedToReset(true);
            DoStateTransition(SelectionState.Selected, false);
        }
        else
        {
            // ¬торой клик - подтверждение выбора
            IsAwaitingConfirmation = false;
            firstPressReceived = false;

            if (pendingButton == this)
                pendingButton = null;

            base.OnPointerClick(eventData);

            characteristics.ApplyChanges();
            characteristics.SetCelectedModule(this);

            DoStateTransition(SelectionState.Normal, false);
        }
    }

    public void ResetConfirmation()
    {
        firstPressReceived = false;
        IsAwaitingConfirmation = false;

        if (pendingButton == this)
            pendingButton = null;

        if (characteristics != null)
        {
            characteristics.SetNeedToReset(true);
        }

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